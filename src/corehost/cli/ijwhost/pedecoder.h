// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#ifndef PEDECODER_H
#define PEDECODER_H

#include "pal.h"
#include "corhdr.h"

#define READYTORUN_SIGNATURE 0x00525452 // 'RTR'
struct READYTORUN_HEADER
{
    DWORD                   Signature;      // READYTORUN_SIGNATURE
    USHORT                  MajorVersion;   // READYTORUN_VERSION_XXX
    USHORT                  MinorVersion;

    DWORD                   Flags;          // READYTORUN_FLAG_XXX

    DWORD                   NumberOfSections;
};

// A subsection of the PEDecoder from CoreCLR that has only the methods we need.
class PEDecoder
{
public:
    PEDecoder(void* mappedBase)
        :m_base((std::uintptr_t)mappedBase)
    {
    }

	bool HasCorHeader() const
    {
        return HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_COMHEADER);
    }

	bool IsILOnly() const
    {
        return((GetCorHeader()->Flags & (std::int32_t)(COMIMAGE_FLAGS_ILONLY)) != 0) || HasReadyToRunHeader();
    }

	bool HasManagedEntryPoint() const;
	bool HasNativeEntryPoint() const;
	void* GetNativeEntryPoint() const;
    IMAGE_COR_VTABLEFIXUP* GetVTableFixups(size_t* numFixupRecords) const;

    HINSTANCE GetBase() const
    {
        return (HINSTANCE)m_base;
    }
    
    std::uintptr_t GetRvaData(std::int32_t rva) const
    {
        if (rva == 0)
        {
            return (std::uintptr_t)nullptr;
        }

        std::int32_t offset = RvaToOffset(rva);

        return m_base + offset;
    }

private:

    bool HasReadyToRunHeader() const
    {
        return FindReadyToRunHeader() != nullptr;
    }

    READYTORUN_HEADER * PEDecoder::FindReadyToRunHeader() const
    {
        IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->ManagedNativeHeader;

        if ((std::int32_t)(pDir->Size) >= sizeof(READYTORUN_HEADER) && CheckDirectory(pDir))
        {
            READYTORUN_HEADER* pHeader = (READYTORUN_HEADER*)((std::uintptr_t)GetDirectoryData(pDir));
            if (pHeader->Signature == READYTORUN_SIGNATURE)
            {
                return pHeader;
            }
        }

        return nullptr;
    }

    bool CheckDirectory(IMAGE_DATA_DIRECTORY *pDir) const
    {
        return CheckRva((std::int32_t)(pDir->VirtualAddress), (std::int32_t)(pDir->Size));
    }

    bool CheckRva(std::int32_t rva, std::size_t size) const;

    bool CheckBounds(std::int32_t rangeBase, std::size_t rangeSize, std::int32_t rva, std::int32_t size) const
    {
        return CheckOverflow(rangeBase, rangeSize)
            && CheckOverflow(rva, size)
            && rva >= rangeBase
            && rva + size <= rangeBase + rangeSize;
    }

    bool CheckOverflow(std::int32_t val1, std::size_t val2) const
    {
        return val1 + val2 >= val1;
    }

    std::size_t RvaToOffset(std::int32_t rva) const
    {
        if (rva > 0)
        {
            IMAGE_SECTION_HEADER* section = RvaToSection(rva);
            if (section == nullptr)
            {
                return rva;
            }

            return rva - (std::int32_t)section->VirtualAddress + (std::int32_t)(section->PointerToRawData);
        }
        return 0;
    }

    IMAGE_SECTION_HEADER* RvaToSection(std::int32_t rva) const;

    static IMAGE_SECTION_HEADER* FindFirstSection(IMAGE_NT_HEADERS* pNTHeaders)
    {
        return reinterpret_cast<IMAGE_SECTION_HEADER*>(
            reinterpret_cast<std::uintptr_t>(pNTHeaders) +
            offsetof(IMAGE_NT_HEADERS, OptionalHeader) +
            (std::int16_t)(pNTHeaders->FileHeader.SizeOfOptionalHeader)
        );
    }

    static std::uint32_t AlignUp(std::uint32_t value, std::uint32_t alignment)
    {
        return (value+alignment-1)&~(alignment-1);
    }


    bool HasDirectoryEntry(int entry) const
    {
        if (Has32BitNTHeaders())
            return (GetNTHeaders32()->OptionalHeader.DataDirectory[entry].VirtualAddress != 0);
        else
            return (GetNTHeaders64()->OptionalHeader.DataDirectory[entry].VirtualAddress != 0);
    }

    IMAGE_NT_HEADERS32* GetNTHeaders32() const
    {
        return reinterpret_cast<IMAGE_NT_HEADERS32*>(FindNTHeaders());
    }

    IMAGE_NT_HEADERS64* GetNTHeaders64() const
    {
        return reinterpret_cast<IMAGE_NT_HEADERS64*>(FindNTHeaders());
    }

    bool Has32BitNTHeaders() const
    {
        return FindNTHeaders()->OptionalHeader.Magic == (std::int16_t)IMAGE_NT_OPTIONAL_HDR32_MAGIC;
    }

    IMAGE_NT_HEADERS* FindNTHeaders() const
    {
        return reinterpret_cast<IMAGE_NT_HEADERS*>(m_base + (reinterpret_cast<IMAGE_DOS_HEADER*>(m_base)->e_lfanew));
    }

    IMAGE_COR20_HEADER *GetCorHeader() const
    {
        return FindCorHeader();
    }

    inline IMAGE_COR20_HEADER *FindCorHeader() const
    {
        return reinterpret_cast<IMAGE_COR20_HEADER*>(GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_COMHEADER));
    }

    IMAGE_DATA_DIRECTORY *GetDirectoryEntry(int entry) const
    {
        if (Has32BitNTHeaders())
            return reinterpret_cast<IMAGE_DATA_DIRECTORY*>(
                reinterpret_cast<std::uintptr_t>(GetNTHeaders32()) +
                offsetof(IMAGE_NT_HEADERS32, OptionalHeader.DataDirectory) +
                entry * sizeof(IMAGE_DATA_DIRECTORY));
        else
            return reinterpret_cast<IMAGE_DATA_DIRECTORY*>(
                reinterpret_cast<std::uintptr_t>(GetNTHeaders64()) +
                offsetof(IMAGE_NT_HEADERS64, OptionalHeader.DataDirectory) +
                entry * sizeof(IMAGE_DATA_DIRECTORY));
    }

    std::uintptr_t GetDirectoryEntryData(int entry, size_t* pSize = nullptr) const
    {
        IMAGE_DATA_DIRECTORY *pDir = GetDirectoryEntry(entry);

        if (pSize != nullptr)
            *pSize = (std::int32_t)(pDir->Size);

        return GetDirectoryData(pDir);
    }

    std::uintptr_t PEDecoder::GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir) const
    {
        return GetRvaData((std::int32_t)(pDir->VirtualAddress));
    }

    ULONG PEDecoder::GetEntryPointToken() const
    {
        return (std::int32_t)(GetCorHeader()->EntryPointToken);
    }

    std::uintptr_t m_base;
};

#endif
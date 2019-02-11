// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pedecoder.h"

namespace
{
    std::uint32_t AlignUp(std::uint32_t value, std::uint32_t alignment)
    {
        return (value+alignment-1)&~(alignment-1);
    }

    bool CheckBounds(std::uint32_t rangeBase, std::uint32_t rangeSize, std::uint32_t rva, std::uint32_t size)
    {
        return CheckOverflow(rangeBase, rangeSize)
            && CheckOverflow(rva, size)
            && rva >= rangeBase
            && rva + size <= rangeBase + rangeSize;
    }

    bool CheckOverflow(std::uint32_t val1, std::uint32_t val2)
    {
        return val1 + val2 >= val1;
    } 
}


bool PEDecoder::HasManagedEntryPoint() const
{
    ULONG flags = GetCorHeader()->Flags;
    return (!(flags & (std::uint32_t)(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (!IsNilToken(GetEntryPointToken())));
}

IMAGE_COR_VTABLEFIXUP *PEDecoder::GetVTableFixups(std::size_t *pCount) const
{
    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->VTableFixups;

    if (pCount != NULL)
        *pCount = pDir->Size / sizeof(IMAGE_COR_VTABLEFIXUP);

    return (IMAGE_COR_VTABLEFIXUP*)(GetDirectoryData(pDir));
}

bool PEDecoder::HasNativeEntryPoint() const
{
    DWORD flags = GetCorHeader()->Flags;
    return ((flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT) &&
            (GetCorHeader()->EntryPointToken != 0));
}

void *PEDecoder::GetNativeEntryPoint() const
{
    return ((void *) GetRvaData(GetCorHeader()->EntryPointToken));
}

bool PEDecoder::CheckRva(std::uint32_t rva, std::uint32_t size) const
{
    if (rva == 0)
    {
        return size == 0;
    }
    else
    {
        IMAGE_SECTION_HEADER *section = RvaToSection(rva);

        if (section == nullptr)
        {
            return false;
        }

        if (!CheckBounds(section->VirtualAddress,
                        section->Misc.VirtualSize,
                        rva, size))
        {
            return false;    
        }

        return CheckBounds(section->VirtualAddress, section->SizeOfRawData, rva, size);
    }
    
    return true;
}

IMAGE_SECTION_HEADER* PEDecoder::RvaToSection(std::uint32_t rva) const
{
    IMAGE_SECTION_HEADER* section = FindFirstSection(FindNTHeaders());
    IMAGE_SECTION_HEADER* sectionEnd = section + FindNTHeaders()->FileHeader.NumberOfSections;

    while (section < sectionEnd)
    {
        if (rva < (section->VirtualAddress
                + AlignUp(section->Misc.VirtualSize, FindNTHeaders()->OptionalHeader.SectionAlignment)))
        {
            if (rva < section->VirtualAddress)
                return nullptr;
            else
            {
                return section;
            }
        }

        section++;
    }

    return nullptr;
}

#define READYTORUN_SIGNATURE 0x00525452 // 'RTR'
struct READYTORUN_HEADER
{
    DWORD                   Signature;      // READYTORUN_SIGNATURE
    USHORT                  MajorVersion;   // READYTORUN_VERSION_XXX
    USHORT                  MinorVersion;

    DWORD                   Flags;          // READYTORUN_FLAG_XXX

    DWORD                   NumberOfSections;
};

READYTORUN_HEADER* PEDecoder::FindReadyToRunHeader() const
{
    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->ManagedNativeHeader;

    if (pDir->Size >= sizeof(READYTORUN_HEADER) && CheckDirectory(pDir))
    {
        READYTORUN_HEADER* pHeader = reinterpret_cast<READYTORUN_HEADER*>(GetDirectoryData(pDir));
        if (pHeader->Signature == READYTORUN_SIGNATURE)
        {
            return pHeader;
        }
    }

    return nullptr;
}
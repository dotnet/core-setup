// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pedecoder.h"

bool PEDecoder::HasManagedEntryPoint() const
{
    ULONG flags = GetCorHeader()->Flags;
    return (!(flags & (std::int32_t)(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
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
    return ((flags & (std::int32_t)(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (GetCorHeader()->EntryPointToken != (std::int32_t)(0)));
}

void *PEDecoder::GetNativeEntryPoint() const
{
    return ((void *) GetRvaData((std::int32_t)(GetCorHeader()->EntryPointToken)));
}

bool PEDecoder::CheckRva(std::int32_t rva, std::size_t size) const
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

        if (!CheckBounds((std::int32_t)section->VirtualAddress,
                        (std::uint32_t)section->Misc.VirtualSize,
                        rva, size))
        {
            return false;    
        }

        return CheckBounds((std::int32_t)section->VirtualAddress, (std::int32_t)section->SizeOfRawData, rva, size);
    }
    
    return true;
}

IMAGE_SECTION_HEADER* PEDecoder::RvaToSection(std::int32_t rva) const
{
    IMAGE_SECTION_HEADER* section = reinterpret_cast<IMAGE_SECTION_HEADER*>(FindFirstSection(FindNTHeaders()));
    IMAGE_SECTION_HEADER* sectionEnd = section + (std::int16_t)FindNTHeaders()->FileHeader.NumberOfSections;

    while (section < sectionEnd)
    {
        if (rva < ((std::int32_t)section->VirtualAddress
                + AlignUp((std::uint32_t)section->Misc.VirtualSize, (std::uint32_t)FindNTHeaders()->OptionalHeader.SectionAlignment)))
        {
            if (rva < (std::int32_t)section->VirtualAddress)
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


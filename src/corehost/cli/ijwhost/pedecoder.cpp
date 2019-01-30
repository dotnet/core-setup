// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pedecoder.h"

using CHECK = BOOL;

#define CHECK(x) if(!(x)) { return FALSE; }
#define CHECK_OK return TRUE

HRESULT PEDecoder::Init(void *mappedBase, bool fixedUp /*= FALSE*/)
{
    m_base = (std::uintptr_t)mappedBase;
    m_flags = FLAG_MAPPED | FLAG_CONTENTS;
    if (fixedUp)
        m_flags |= FLAG_RELOCATED;
    return S_OK;
}

BOOL PEDecoder::HasManagedEntryPoint() const
{
    ULONG flags = GetCorHeader()->Flags;
    return (!(flags & (std::int32_t)(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (!IsNilToken(GetEntryPointToken())));
}

IMAGE_COR_VTABLEFIXUP *PEDecoder::GetVTableFixups(std::size_t *pCount) const
{
    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->VTableFixups;

    if (pCount != NULL)
        *pCount = (std::int32_t)(pDir->Size)/sizeof(IMAGE_COR_VTABLEFIXUP);

    return (IMAGE_COR_VTABLEFIXUP*)(GetDirectoryData(pDir));
}

BOOL PEDecoder::HasNativeEntryPoint() const
{
    ULONG flags = GetCorHeader()->Flags;
    return ((flags & (std::int32_t)(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (GetCorHeader()->EntryPointToken != (std::int32_t)(0)));
}

void *PEDecoder::GetNativeEntryPoint() const
{
    return ((void *) GetRvaData((std::int32_t)(GetCorHeader()->EntryPointToken)));
}

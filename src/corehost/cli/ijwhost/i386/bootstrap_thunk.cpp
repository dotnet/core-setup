// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "corhdr.h"
#include "bootstrap_thunk_chunk.h"

//=================================================================================
// Get thunk from the return address that the call instruction would have pushed
bootstrap_thunk *bootstrap_thunk::get_thunk_from_cookie(std::uintptr_t cookie)
{
    return (bootstrap_thunk *)(cookie - offsetof(bootstrap_thunk, m_dll));
}

//=================================================================================
//
bootstrap_thunk *bootstrap_thunk::get_thunk_from_entrypoint(std::uintptr_t entryAddr)
{
    return (bootstrap_thunk *)(entryAddr - offsetof(bootstrap_thunk, m_code));
}

//=================================================================================
// Returns the slot address of the vtable entry for this thunk
std::uintptr_t *bootstrap_thunk::get_slot_address()
{
    return (std::uintptr_t *)((std::uintptr_t)m_slot & ~1);
}

//=================================================================================
// Returns the pal::dll_t for this thunk's module
pal::dll_t bootstrap_thunk::get_dll_handle()
{
    return m_dll;
}

//=================================================================================
// Returns the token of this thunk
std::uint32_t bootstrap_thunk::get_token()
{
    std::uint32_t ulTok = 0;
    BYTE *pbTok = (BYTE *)&ulTok;

    memcpy(pbTok, &m_tok[0], 3);

    if (((std::uintptr_t)m_slot & 0x1))
        ulTok |= mdtMethodDef;
    else
        ulTok |= mdtMemberRef;

    return ulTok;
}

//=================================================================================
std::uintptr_t bootstrap_thunk::get_entrypoint()
{
    return (std::uintptr_t)this + offsetof(bootstrap_thunk, m_code);
}

//=================================================================================
// Initializes the thunk to point to the bootstrap helper that will load the
// runtime and perform the real thunk initialization.
//
void bootstrap_thunk::initialize(std::uintptr_t pThunkInitFcn,
                                          pal::dll_t dll,
                                          std::uint32_t token,
                                          std::uintptr_t *pSlot)
{
    
    // First fill in the token portion of the struct.
    BYTE *pbTok = (BYTE *)(&token);
    memcpy(&m_tok[0], pbTok, 3);

    // Now set up the thunk code
    std::uintptr_t pFrom;
    std::uintptr_t pTo;
    
    // This is the call to the thunk bootstrapper function
    pFrom = (std::uintptr_t)&m_code.m_thunkFcn + sizeof(m_code.m_thunkFcn);
    pTo = pThunkInitFcn;
    m_code.m_call            = 0xe8;
    m_code.m_thunkFcn        = (UINT32)(pTo - pFrom);

    // Fill out the rest of the info
    m_dll = dll;
    m_slot = pSlot;

    assert(TypeFromToken(token) == mdtMethodDef ||
             TypeFromToken(token) == mdtMemberRef);
    assert(!((std::uintptr_t)m_slot & 0x1));

    if (TypeFromToken(token) == mdtMethodDef)
        m_slot = (std::uintptr_t *)((std::uintptr_t)m_slot | 0x1);
}


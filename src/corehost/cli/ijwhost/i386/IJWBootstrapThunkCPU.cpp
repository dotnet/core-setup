// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pedecoder.h"
#include "IJWBootstrapThunkCPU.h"

#include <pshpack1.h>
class VTableBootstrapThunkCode
{
private:
    BYTE            m_tok[3];

    struct {
        BYTE            m_call;         //0xe8
        UINT32          m_thunkFcn;     //bootstrapper function
    } m_code;

    //@TODO: These will both be removed in the future.
    pal::dll_t       m_dll;            // pal::dll_t of this module
    std::uintptr_t       *m_slot;             // VTable slot for this thunk

public:
    // Get thunk from the return address that the call instruction would have pushed
    static VTableBootstrapThunkCode *GetThunkFromCookie(std::uintptr_t cookie);

    // Get thunk from the return address that the call instruction would have pushed
    static VTableBootstrapThunkCode *GetThunkFromEntrypoint(std::uintptr_t entryAddr);

    // Initializes the thunk to point to pThunkInitFcn that will load the
    // runtime and perform the real thunk initialization.
    void Initialize(std::uintptr_t pThunkInitFcn,
                    pal::dll_t dll,
                    std::uint32_t token,
                    std::uintptr_t *pSlot);

    // Returns the slot address of the vtable entry for this thunk
    std::uintptr_t *GetSlotAddr();

    // Returns the pal::dll_t for this thunk's module
    pal::dll_t GetDLLHandle();

    // Returns the token of this thunk
    std::uint32_t GetToken();

    std::uintptr_t GetEntrypoint();
};
#include <poppack.h>

// Returns a pointer to the callable code
VTableBootstrapThunkCode *VTableBootstrapThunk::GetCode()
{
    return (VTableBootstrapThunkCode *)this;
}

//=================================================================================
// Get thunk from the return address that the call instruction would have pushed
VTableBootstrapThunk *VTableBootstrapThunk::GetThunkFromCookie(std::uintptr_t cookie)
{
    return (VTableBootstrapThunk *)(VTableBootstrapThunkCode::GetThunkFromCookie(cookie));
}

//=================================================================================
// Get thunk from the return address that the call instruction would have pushed
VTableBootstrapThunkCode *VTableBootstrapThunkCode::GetThunkFromCookie(std::uintptr_t cookie)
{
    return (VTableBootstrapThunkCode *)(cookie - offsetof(VTableBootstrapThunkCode, m_dll));
}

//=================================================================================
//
VTableBootstrapThunk *VTableBootstrapThunk::GetThunkFromEntrypoint(std::uintptr_t entryAddr)
{
    return (VTableBootstrapThunk *)(VTableBootstrapThunkCode::GetThunkFromEntrypoint(entryAddr));
}

//=================================================================================
// Gets the object size to allocate. Must use this instead of sizeof()
size_t VTableBootstrapThunk::GetThunkObjectSize()
{
    return sizeof(VTableBootstrapThunkCode);
}

//=================================================================================
//
VTableBootstrapThunkCode *VTableBootstrapThunkCode::GetThunkFromEntrypoint(std::uintptr_t entryAddr)
{
    return (VTableBootstrapThunkCode *)(entryAddr - offsetof(VTableBootstrapThunkCode, m_code));
}

//=================================================================================
// Returns the slot address of the vtable entry for this thunk
std::uintptr_t *VTableBootstrapThunkCode::GetSlotAddr()
{
    return (std::uintptr_t *)((std::uintptr_t)m_slot & ~1);
}

//=================================================================================
// Returns the slot address of the vtable entry for this thunk
std::uintptr_t *VTableBootstrapThunk::GetSlotAddr()
{
    return GetCode()->GetSlotAddr();
}

//=================================================================================
// Returns the pal::dll_t for this thunk's module
pal::dll_t VTableBootstrapThunk::GetDLLHandle()
{
    return GetCode()->GetDLLHandle();
}

//=================================================================================
// Returns the pal::dll_t for this thunk's module
pal::dll_t VTableBootstrapThunkCode::GetDLLHandle()
{
    return m_dll;
}

//=================================================================================
// Returns the token of this thunk
std::uint32_t VTableBootstrapThunkCode::GetToken()
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
// Returns the token of this thunk
std::uint32_t VTableBootstrapThunk::GetToken()
{
    return GetCode()->GetToken();
}

//=================================================================================
std::uintptr_t VTableBootstrapThunkCode::GetEntrypoint()
{
    return (std::uintptr_t)this + offsetof(VTableBootstrapThunkCode, m_code);
}

//=================================================================================
std::uintptr_t VTableBootstrapThunk::GetEntrypoint()
{
    return GetCode()->GetEntrypoint();
}

//=================================================================================
// Ctor
VTableBootstrapThunkChunk::VTableBootstrapThunkChunk(size_t numThunks, pal::dll_t dll)
    : m_numThunks(numThunks), m_dll(dll), m_next(NULL)
{
#ifdef _DEBUG
    memset(m_thunks, 0, m_numThunks * VTableBootstrapThunk::GetThunkObjectSize());
#endif
}

//=================================================================================
// Returns the VTableBootstrapThunk at the given index.
VTableBootstrapThunk *VTableBootstrapThunkChunk::GetThunk(size_t idx)
{
    return (VTableBootstrapThunk *)((std::uintptr_t)m_thunks + (idx * VTableBootstrapThunk::GetThunkObjectSize()));
}

//=================================================================================
// Returns the pal::dll_t for this module
pal::dll_t VTableBootstrapThunkChunk::GetDLLHandle()
{
    return m_dll;
}

//=================================================================================
//
VTableBootstrapThunkChunk *VTableBootstrapThunkChunk::GetNext()
{
    return m_next;
}

//=================================================================================
//
VTableBootstrapThunkChunk **VTableBootstrapThunkChunk::GetNextPtr()
{
    return &m_next;
}

//=================================================================================
//
void VTableBootstrapThunkChunk::SetNext(VTableBootstrapThunkChunk *pNext)
{
    m_next = pNext;
}

//=================================================================================
void VTableBootstrapThunk::Initialize(std::uintptr_t pThunkInitFcn,
                                      pal::dll_t dll,
                                      std::uint32_t token,
                                      std::uintptr_t *pSlot)
{
    ((VTableBootstrapThunkCode *)this)->Initialize(pThunkInitFcn,
                                                   dll,
                                                   token,
                                                   pSlot);
}

//=================================================================================
// Initializes the thunk to point to the bootstrap helper that will load the
// runtime and perform the real thunk initialization.
//
void VTableBootstrapThunkCode::Initialize(std::uintptr_t pThunkInitFcn,
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
    m_code.m_thunkFcn        = (UINT32)(pTo - pFrom);// _ASSERTE(FitsInU4(pTo - pFrom));

    // Fill out the rest of the info
    //@TODO: These will both be removed in the future.
    m_dll = dll;
    m_slot = pSlot;

    _ASSERTE(TypeFromToken(token) == mdtMethodDef ||
             TypeFromToken(token) == mdtMemberRef);
    _ASSERTE(!((std::uintptr_t)m_slot & 0x1));

    if (TypeFromToken(token) == mdtMethodDef)
        m_slot = (std::uintptr_t *)((std::uintptr_t)m_slot | 0x1);
}


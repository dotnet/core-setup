// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pedecoder.h"
#include "IJWBootstrapThunkCPU.h"

#include <pshpack1.h>
class VTableBootstrapThunkCode
{
private:
    // 49 BA 78 56 34 12 78 56 34 12 mov         r10,1234567812345678h
    // 49 BB 34 12 34 12 34 12 34 12 mov         r11,1234123412341234h
    // 41 FF E3                      jmp         r11

    static BYTE             s_mov_r10[2];
    static BYTE             s_mov_r11[2];
    static BYTE             s_jmp_r11[3];

    BYTE                    m_mov_r10[2];
    BYTE                    m_val_r10[8];
    BYTE                    m_mov_r11[2];
    BYTE                    m_val_r11[8];
    BYTE                    m_jmp_r11[3];   // total 23 bytes
    BYTE                    m_padding[1];   // 1 byte to pad to 24
    // Data for the thunk
    mdToken                 m_token;                        // 4 bytes
    enum {
        e_TOKEN_IS_DEF = 0x1
    };
    UINT32                  m_flags;                        // 4 bytes

    pal::dll_t               m_dll;            // pal::dll_t of this module
                                                    // 8 bytes
    std::uintptr_t               *m_slot;             // VTable slot for this thunk
                                                    // 8 bytes
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

BYTE VTableBootstrapThunkCode::s_mov_r10[2] = {0x49, 0xBA};
BYTE VTableBootstrapThunkCode::s_mov_r11[2] = {0x49, 0xBB};
BYTE VTableBootstrapThunkCode::s_jmp_r11[3] = {0x41, 0xFF, 0xE3};

//=================================================================================
// Returns a pointer to the callable code
VTableBootstrapThunkCode *VTableBootstrapThunk::GetCode()
{
        return (VTableBootstrapThunkCode *)this;
}

//=================================================================================
// Gets the object size to allocate. Must use this instead of sizeof()
size_t VTableBootstrapThunk::GetThunkObjectSize()
{
    return sizeof(VTableBootstrapThunkCode);
}

//=================================================================================
// Get thunk from the return address that the call instruction would have pushed
VTableBootstrapThunkCode *VTableBootstrapThunkCode::GetThunkFromCookie(std::uintptr_t cookie)
{
    return (VTableBootstrapThunkCode *)cookie;
}

//=================================================================================
// Get thunk from the return address that the call instruction would have pushed
VTableBootstrapThunk *VTableBootstrapThunk::GetThunkFromCookie(std::uintptr_t cookie)
{
    return (VTableBootstrapThunk *)(VTableBootstrapThunkCode::GetThunkFromCookie(cookie));
}

//=================================================================================
//
VTableBootstrapThunkCode *VTableBootstrapThunkCode::GetThunkFromEntrypoint(std::uintptr_t entryAddr)
{
    return (VTableBootstrapThunkCode *)
        ((std::uintptr_t)entryAddr - offsetof(VTableBootstrapThunkCode, m_mov_r10));
}

//=================================================================================
//
VTableBootstrapThunk *VTableBootstrapThunk::GetThunkFromEntrypoint(std::uintptr_t entryAddr)
{
    return (VTableBootstrapThunk *)(VTableBootstrapThunkCode::GetThunkFromEntrypoint(entryAddr));
}

//=================================================================================
// Returns the slot address of the vtable entry for this thunk
std::uintptr_t *VTableBootstrapThunkCode::GetSlotAddr()
{
    return m_slot;
}

//=================================================================================
// Returns the slot address of the vtable entry for this thunk
std::uintptr_t *VTableBootstrapThunk::GetSlotAddr()
{
    return GetCode()->GetSlotAddr();
}

//=================================================================================
// Returns the pal::dll_t for this thunk's module
pal::dll_t VTableBootstrapThunkCode::GetDLLHandle()
{
    return m_dll;
}

//=================================================================================
// Returns the pal::dll_t for this thunk's module
pal::dll_t VTableBootstrapThunk::GetDLLHandle()
{
    return GetCode()->GetDLLHandle();
}

//=================================================================================
// Returns the token of this thunk
std::uint32_t VTableBootstrapThunkCode::GetToken()
{
    return m_token;
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
    return (std::uintptr_t)&m_mov_r10[0];
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
// Initializes the thunk to point to the bootstrap helper that will load the
// runtime and perform the real thunk initialization.
//
void VTableBootstrapThunkCode::Initialize(std::uintptr_t pThunkInitFcn,
                                          pal::dll_t dll,
                                          std::uint32_t token,
                                          std::uintptr_t *pSlot)
{
    // Initialize the jump thunk.
    memcpy(&m_mov_r10[0], &s_mov_r10[0], sizeof(s_mov_r10));
    (*((void **)&m_val_r10[0])) = (void *)this;
    memcpy(&m_mov_r11[0], &s_mov_r11[0], sizeof(s_mov_r11));
    (*((void **)&m_val_r11[0])) = (void *)pThunkInitFcn;
    memcpy(&m_jmp_r11[0], &s_jmp_r11[0], sizeof(s_jmp_r11));

    // Fill out the rest of the info
    m_token = token;
    m_dll = dll;
    m_slot = pSlot;
    m_flags = 0;

    _ASSERTE(TypeFromToken(token) == mdtMethodDef ||
             TypeFromToken(token) == mdtMemberRef);

    if (TypeFromToken(token) == mdtMethodDef)
        m_flags = e_TOKEN_IS_DEF;
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


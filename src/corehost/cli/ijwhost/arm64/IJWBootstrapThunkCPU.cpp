// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "pedecoder.h"
#include "IJWBootstrapThunkCPU.h"

class VTableBootstrapThunkCode
{
private:
    DWORD           m_rgCode[4];
    std::uintptr_t        m_pBootstrapCode;

    pal::dll_t       m_dll;            // pal::dll_t of this module
    std::uintptr_t       *m_slot;             // VTable slot for this thunk
    std::uint32_t           m_token;            // Token for this thunk

public:
    // Get thunk from the address that the thunk code provided
    static VTableBootstrapThunkCode *GetThunkFromCookie(std::uintptr_t cookie);

    // Get thunk from the thunk code entry point address
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

//=================================================================================
// Returns a pointer to the callable code
VTableBootstrapThunkCode *VTableBootstrapThunk::GetCode()
{
        return (VTableBootstrapThunkCode *)this;
}

//=================================================================================
// Get thunk from the address that the thunk code provided
VTableBootstrapThunk *VTableBootstrapThunk::GetThunkFromCookie(std::uintptr_t cookie)
{
        return (VTableBootstrapThunk *)(VTableBootstrapThunkCode::GetThunkFromCookie(cookie));
}

//=================================================================================
// Get thunk from the address that the thunk code provided
VTableBootstrapThunkCode *VTableBootstrapThunkCode::GetThunkFromCookie(std::uintptr_t cookie)
{
    
    // Cookie is generated via the first thunk instruction:
    //  mov r12, pc
    // The pc is returned from the hardware as the pc at the start of the instruction (i.e. the thunk address)
    // + 4. So we can recover the thunk address simply by subtracting 4 from the cookie.
    return (VTableBootstrapThunkCode *)(cookie - 4);
}

//=================================================================================
// Get thunk from the thunk code entry point address
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
// Get thunk from the thunk code entry point address
VTableBootstrapThunkCode *VTableBootstrapThunkCode::GetThunkFromEntrypoint(std::uintptr_t entryAddr)
{
        // The entry point is at the start of the thunk but the code address will have the low-order bit set to
    // indicate Thumb code and we need to mask that out.
    return (VTableBootstrapThunkCode *)(entryAddr & ~1);
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
    // Set the low-order bit of the address returned to indicate to the hardware that it's Thumb code.
    return (std::uintptr_t)this | 1;
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
    // Initialize code section of the thunk:
    WORD rgCode[] = {
        0x46fc,             // mov r12, pc
        0xf8df, 0xf004,     // ldr pc, [pc, #4]
        0x0000              // padding for 4-byte alignment of target address that follows
    };
    BYTE *pCode = (BYTE*)this;
    memcpy(pCode, rgCode, sizeof(rgCode));
    pCode += sizeof(rgCode);
    *(std::uintptr_t*)pCode = pThunkInitFcn;

    m_dll = dll;
    m_slot = pSlot;
    m_token = token;
}

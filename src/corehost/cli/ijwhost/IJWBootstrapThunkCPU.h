// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __IJWTHUNK_H__
#define __IJWTHUNK_H__

#include "pal.h"

extern "C" void VTableBootstrapThunkInitHelperStub();

class VTableBootstrapThunkCode;

class VTableBootstrapThunk
{
private:
    VTableBootstrapThunkCode *GetCode();

public:
    // Get thunk from the return address that the call instruction would have pushed
    static VTableBootstrapThunk *GetThunkFromCookie(std::uintptr_t cookie);

    // Get thunk from the return address that the call instruction would have pushed
    static VTableBootstrapThunk *GetThunkFromEntrypoint(std::uintptr_t entryAddr);

    // Gets the object size to allocate. Must use this instead of sizeof()
    static size_t GetThunkObjectSize();

    // Initializes the thunk to point to pThunkInitFcn that will load the
    // runtime and perform the real thunk initialization.
    void Initialize(std::uintptr_t pThunkInitFcn,
                    pal::dll_t dll,
                    std::uint32_t token,
                    std::uintptr_t *pSlot);

    // Returns the pal::dll_t for this thunk's module
    pal::dll_t GetDLLHandle();

    // Returns the token of this thunk
    std::uint32_t GetToken();

    // Returns pointer to callable code.
    std::uintptr_t GetEntrypoint();

    // Returns the slot that this thunk represents.
    std::uintptr_t *GetSlotAddr();
};

#include <pshpack1.h>
class VTableBootstrapThunkChunk
{
private:
    pal::dll_t                  m_dll;
    size_t                     m_numThunks;
    VTableBootstrapThunkChunk *m_next;
    VTableBootstrapThunk       m_thunks[0];

public:
    // Ctor
    VTableBootstrapThunkChunk(size_t numThunks, pal::dll_t dll);

    // Returns the VTableBootstrapThunk at the given index.
    VTableBootstrapThunk *GetThunk(size_t idx);

    // Returns the pal::dll_t for this module
    pal::dll_t GetDLLHandle();

    // Linked list of thunk chunks (one per loaded module)
    VTableBootstrapThunkChunk *GetNext();
    VTableBootstrapThunkChunk **GetNextPtr();
    void SetNext(VTableBootstrapThunkChunk *pNext);
};
#include <poppack.h>

#endif // __IJWTHUNK_H__

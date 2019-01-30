// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "ijwhost.h"
#include "IJWBootstrapThunkCPU.h"
#include "corhdr.h"
#include "executableheap.h"
#include <new>
#include <mutex>

#ifdef _WIN64
#define COR_VTABLE_PTRSIZED     COR_VTABLE_64BIT
#define COR_VTABLE_NOT_PTRSIZED COR_VTABLE_32BIT
#else
#define COR_VTABLE_PTRSIZED     COR_VTABLE_32BIT
#define COR_VTABLE_NOT_PTRSIZED COR_VTABLE_64BIT
#endif

extern "C" std::uintptr_t __stdcall VTableBootstrapThunkInitHelper(std::uintptr_t cookie);

std::mutex g_thunkChunkLock;
VTableBootstrapThunkChunk* g_pVtableBootstrapThunkChunkList;

bool PatchVTableEntriesForDLLAttach(PEDecoder& pe)
{
    if (pe.IsILOnly())
    {
        // Nothing to do if the PE is IL-only.
        return true;
    }
    
    size_t numFixupRecords;
    IMAGE_COR_VTABLEFIXUP* pFixupTable = pe.GetVTableFixups(&numFixupRecords);

    if (numFixupRecords == 0)
    {
        // If we have no fixups, no need to allocate thunks.
        return true;
    }

    size_t numThunks = 0;
    for (size_t i = 0; i < numFixupRecords; ++i)
    {
        numThunks += pFixupTable[i].Count;
    }

    size_t chunkSize = sizeof(VTableBootstrapThunkChunk) + VTableBootstrapThunk::GetThunkObjectSize() * numThunks;

    void* pbChunk = AllocateExecutable(chunkSize);

    if (pbChunk == nullptr)
    {
        return false;
    }

    VTableBootstrapThunkChunk* chunk = new (pbChunk) VTableBootstrapThunkChunk(numThunks, (pal::dll_t)pe.GetBase());

    {
        std::lock_guard<std::mutex> _(g_thunkChunkLock);
        chunk->SetNext(g_pVtableBootstrapThunkChunkList);
        g_pVtableBootstrapThunkChunkList = chunk;
    }

    size_t currentThunk = 0;
    for(size_t i = 0; i < numFixupRecords; ++i)
    {
#ifndef _WIN64
        assert((pFixupTable[i].Type & (COR_VTABLE_FROM_UNMANAGED | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN)) && "managed->managed vtablefixup slots are not supported!");
#endif
        if (pFixupTable[i].Type & COR_VTABLE_PTRSIZED)
        {
            const BYTE** pointers = (const BYTE**)pe.GetRvaData(pFixupTable[i].RVA);

#ifdef _WIN64
            DWORD oldProtect;
            if(!VirtualProtect(pointers, (sizeof(BYTE*) * pFixupTable[i].Count), PAGE_READWRITE, &oldProtect))
            {
                // TODO: Make this log instead of assert
                _ASSERTE(!"PatchVTableEntriesForDLLAttach(): VirtualProtect() changing IJW thunk vtable to R/W failed.\n");
                return false;
            }
#endif

            for (std::uint16_t method = 0; method < pFixupTable[i].Count; method++)
            {
                mdToken tok = (mdToken)(std::uintptr_t) pointers[method];
                assert (TypeFromToken(tok) == mdtMethodDef || TypeFromToken(tok) == mdtMemberRef);
                VTableBootstrapThunk* pThunk = chunk->GetThunk(currentThunk++);
                pThunk->Initialize((std::uintptr_t)&VTableBootstrapThunkInitHelperStub,
                                    (pal::dll_t)pe.GetBase(),
                                    tok,
                                    (std::uintptr_t *)&pointers[method]);
                pointers[method] = (BYTE*)pThunk->GetEntrypoint();
            }

#ifdef _WIN64
            DWORD _;
            if(!VirtualProtect(pointers, (sizeof(BYTE*) * pFixupTable[i].Count), oldProtect, &_))
            {
                // TODO: Make this log instead of assert
                _ASSERTE(!"PatchVTableEntriesForDLLAttach(): VirtualProtect() changing IJW thunk vtable back to RO failed.\n");
            }
#endif
        }
    }

    return true;
}

extern "C" std::uintptr_t __stdcall VTableBootstrapThunkInitHelper(std::uintptr_t cookie)
{
    VTableBootstrapThunk *pThunk = VTableBootstrapThunk::GetThunkFromCookie(cookie);
    pal::hresult_t hr = StartRuntimeIfNotStarted(pThunk->GetDLLHandle());
    std::uintptr_t thunkAddress;

    if (FAILED(hr))
    {
        // If we ignore the failure to patch bootstrap thunks we will come to this same
        // function again, causing an infinite loop of "Failed to start the .NET Core runtime" errors.
        // As we were taken here via an entry point with arbitrary signature,
        // there's no way of returning the error code so we just throw it.
#pragma warning (push)
#pragma warning (disable: 4297)
        throw hr;
#pragma warning (pop)
    }
    
    thunkAddress = *(pThunk->GetSlotAddr());

    return thunkAddress;
}


void BootstrapThunkDLLDetach(PEDecoder& pe)
{
    // Is this an IJW module
    if (!pe.IsILOnly())
    {
        std::lock_guard<std::mutex> _(g_thunkChunkLock);
        // Clean up the VTable thunks if they exist.
        for (VTableBootstrapThunkChunk **ppCurChunk = &g_pVtableBootstrapThunkChunkList;
             *ppCurChunk != NULL;
             ppCurChunk = (*ppCurChunk)->GetNextPtr())
        {
            if ((*ppCurChunk)->GetDLLHandle() == (pal::dll_t) pe.GetBase())
            {
                VTableBootstrapThunkChunk *pDel = *ppCurChunk;
                *ppCurChunk = (*ppCurChunk)->GetNext();
                DeallocateExecutable(pDel);
                break;
            }
        }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "pedecoder.h"
#include "ijwhost.h"
#include "IJWBootstrapThunkCPU.h"
#include "error_codes.h"
#include "trace.h"
#include <cassert>

SHARED_API std::int32_t STDMETHODCALLTYPE _CorExeMain()
{
    load_and_execute_in_memory_assembly_fn loadAndExecute;
    std::int32_t status = get_load_and_execute_in_memory_assembly_delegate(&loadAndExecute);
    if (status != StatusCode::Success)
    {
        trace::error(_X("Unable to load .NET Core runtime and get entry-point."));
        return status;
    }

    int argc;
    pal::char_t** argv = CommandLineToArgvW(GetCommandLineW(), &argc);

    status = loadAndExecute(GetModuleHandle(nullptr), argc, argv);

    LocalFree(argv);

    return argc;
}

SHARED_API BOOL STDMETHODCALLTYPE _CorDllMain(HINSTANCE hInst,
    DWORD  dwReason,
    LPVOID lpReserved
)
{
    BOOL res = TRUE;

    PEDecoder pe(hInst);

    // In the following code, want to make sure that we do our own initialization before
    // we call into managed or unmanaged initialization code, and that we perform
    // uninitialization after we call into managed or unmanaged uninitialization code.
    // Thus, we do DLL_PROCESS_ATTACH work first, and DLL_PROCESS_DETACH work last.
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        // If this is not a .NET module (has a CorHeader), shouldn't be calling _CorDllMain
        if (!pe.HasCorHeader())
        {
            return FALSE;
        }

        if (pe.IsILOnly() || (!pe.HasManagedEntryPoint() && !pe.HasNativeEntryPoint()))
        {
            // If there is no user entry point, then we don't want the
            // thread start/stop events going through because it slows down
            // thread creation operations
            DisableThreadLibraryCalls(hInst);
        }

        // Install the bootstrap thunks
        if (!PatchVTableEntriesForDLLAttach(pe))
        {
            return FALSE;
        }
    }

    // Now call the unmanaged entrypoint if it exists
    if (pe.HasNativeEntryPoint())
    {
        DllMain_t pUnmanagedDllMain = (DllMain_t)pe.GetNativeEntryPoint();
        assert(pUnmanagedDllMain != nullptr);
        res = pUnmanagedDllMain(hInst, dwReason, lpReserved);
    }

    if (dwReason == DLL_PROCESS_DETACH)
    {
        BootstrapThunkDLLDetach(pe);
    }

    return res;
}

BOOL STDMETHODCALLTYPE DllMain(HINSTANCE hInst,
    DWORD  dwReason,
    LPVOID lpReserved
)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        g_heapHandle = HeapCreate(HEAP_CREATE_ENABLE_EXECUTE, 0, 0);
        return g_heapHandle != NULL ? TRUE : FALSE;
    case DLL_PROCESS_DETACH:
        HeapDestroy(g_heapHandle);
        break;
    }
    return TRUE;
}

SHARED_API mdToken STDMETHODCALLTYPE GetTokenForVTableEntry(HMODULE hMod, void** ppVTEntry)
{
    mdToken tok = mdTokenNil;
    if (AreThunksInstalledForModule(hMod))
    {
        VTableBootstrapThunk* pThunk =
            VTableBootstrapThunk::GetThunkFromEntrypoint((std::uintptr_t) *ppVTEntry);
        tok = (mdToken) pThunk->GetToken();
    }
    else
    {
        tok = (mdToken)(std::uintptr_t) *ppVTEntry;
    }
    assert(TypeFromToken(tok) == mdtMethodDef || TypeFromToken(tok) == mdtMemberRef);

    return tok;
}

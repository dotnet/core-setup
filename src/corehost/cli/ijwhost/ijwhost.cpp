// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ijwhost.h"
#include "hostfxr.h"
#include "fxr_resolver.h"
#include "pal.h"
#include "trace.h"
#include "error_codes.h"
#include "utils.h"
#include "bootstrap_thunk.h"


#if defined(_WIN32)
// IJW entry points are defined without the __declspec(dllexport) attribute.
// The issue here is that the MSVC compiler links to the exact name _CorDllMain instead of their stdcall-managled names.
// So we need to export the exact name, which __declspec(dllexport) doesn't do. The solution here is to the use a .def file on Windows.
#define IJW_API extern "C"
#else
#define IJW_API SHARED_API
#endif // _WIN32

pal::hresult_t get_load_in_memory_assembly_delegate(pal::dll_t handle, load_in_memory_assembly_fn* delegate)
{
    return load_fxr_and_get_delegate(
        hostfxr_delegate_type::hdt_load_in_memory_assembly,
        [handle](const pal::string_t& host_path, pal::string_t* config_path_out)
        {
            pal::string_t mod_path;
            if (!pal::get_module_path(handle, &mod_path))
            {
                trace::error(_X("Failed to resolve full path of the current mixed-mode module [%s]"), mod_path.c_str());
                return StatusCode::LibHostCurExeFindFailure;
            }

            pal::string_t config_path_local { strip_file_ext(mod_path) };
            config_path_local.append(_X(".runtimeconfig.json"));

            *config_path_out = std::move(config_path_local);

            // if file does not exist, try to get the one used for an existing context (if there is any)
            if (!pal::realpath(&config_path_local))
            {
                trace::info(_X("Failed to resolve .runtimeconfig.json path of the current mixed-mode module [%s]"), mod_path.c_str());
                trace::info(_X("Trying to get .runtimeconfig.json from existing context"));

                pal::dll_t out_fxr;
                pal::string_t out_fxr_path;

                if (pal::get_loaded_library(LIBFXR_NAME, "hostfxr_get_runtime_property_value", &out_fxr, &out_fxr_path))
                {
                    hostfxr_get_runtime_property_value_fn get_runtime_property_value = reinterpret_cast<hostfxr_get_runtime_property_value_fn>(pal::get_symbol(out_fxr, "hostfxr_get_runtime_property_value"));

                    const pal::char_t *value;
                    int rc = get_runtime_property_value(nullptr, _X("APP_CONTEXT_RUNTIME_CONFIG_FILE"), &value);

                    if (rc == StatusCode::Success)
                    {
                        pal::string_t config_path_from_context = value;
                        *config_path_out = std::move(config_path_from_context);

                        trace::info(_X("Got .runtimeconfig.json [%s] from existing context"), config_path_out->c_str());
                        return StatusCode::Success;
                    }
                    else
                    {
                        trace::error(_X("Getting APP_CONTEXT_RUNTIME_CONFIG_FILE from existing context failed with [%i]"), rc);
                    }
                }
                else
                {
                    trace::error(_X("Could not get .runtimeconfig.json from existing context as [%s] was not loaded"), LIBFXR_NAME);
                }

                trace::error(_X("Failed to resolve .runtimeconfig.json path of the current mixed-mode module [%s] and also failed to get it from an existing context"), mod_path.c_str());
                return StatusCode::InvalidConfigFile;
            }

            return StatusCode::Success;
        },
        delegate
    );
}

IJW_API BOOL STDMETHODCALLTYPE _CorDllMain(HINSTANCE hInst,
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

        if (!pe.HasManagedEntryPoint() && !pe.HasNativeEntryPoint())
        {
            // If there is no user entry point, then we don't want the
            // thread start/stop events going through because it slows down
            // thread creation operations
            DisableThreadLibraryCalls(hInst);
        }

        // Install the bootstrap thunks
        if (!patch_vtable_entries(pe))
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
        release_bootstrap_thunks(pe);
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
    mdToken tok;
    if (are_thunks_installed_for_module(hMod))
    {
        bootstrap_thunk* pThunk =
            bootstrap_thunk::get_thunk_from_entrypoint((std::uintptr_t) *ppVTEntry);
        tok = (mdToken) pThunk->get_token();
    }
    else
    {
        tok = (mdToken)(std::uintptr_t) *ppVTEntry;
    }

    return tok;
}

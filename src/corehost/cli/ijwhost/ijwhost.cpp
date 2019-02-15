// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "ijwhost.h"
#include "fxr_resolver.h"
#include "pal.h"
#include "trace.h"
#include "error_codes.h"
#include "utils.h"

namespace
{
    int load_hostfxr_delegate(pal::dll_t moduleHandle, const char* entryPoint, void** delegate)
    {
        pal::dll_t fxr;

        pal::string_t host_path;
        if (!pal::get_own_module_path(&host_path) || !pal::realpath(&host_path))
        {
            trace::error(_X("Failed to resolve full path of the current host module [%s]"), host_path.c_str());
            return StatusCode::CoreHostCurHostFindFailure;
        }

        pal::string_t dotnet_root;
        pal::string_t fxr_path;
        if (!resolve_fxr_path(get_directory(host_path), &dotnet_root, &fxr_path))
        {
            return StatusCode::CoreHostLibMissingFailure;
        }

        // Load library
        if (!pal::load_library(&fxr_path, &fxr))
        {
            trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
            trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
            trace::error(_X("     %s"), DOTNET_CORE_INSTALL_PREREQUISITES_URL);
            return StatusCode::CoreHostLibLoadFailure;
        }

        // Leak fxr

        auto get_delegate_from_hostfxr = (hostfxr_get_delegate_fn)pal::get_symbol(fxr, entryPoint);
        if (get_delegate_from_hostfxr == nullptr)
            return StatusCode::CoreHostEntryPointFailure;

        pal::string_t app_path;

        if (!pal::get_module_path(moduleHandle, &app_path))
        {
            trace::error(_X("Failed to resolve full path of the current mixed-mode module [%s]"), host_path.c_str());
            return StatusCode::LibHostCurExeFindFailure;
        }

        return get_delegate_from_hostfxr(host_path.c_str(), dotnet_root.c_str(), app_path.c_str(), delegate);
    }
}

pal::hresult_t get_load_and_execute_in_memory_assembly_delegate(load_and_execute_in_memory_assembly_fn* delegate)
{
    return load_hostfxr_delegate(GetModuleHandle(nullptr), "hostfxr_get_load_and_execute_in_memory_assembly_delegate", (void**)delegate);
}

pal::hresult_t get_load_in_memory_assembly_delegate(pal::dll_t handle, load_in_memory_assembly_fn* delegate)
{
    return load_hostfxr_delegate(handle, "hostfxr_get_load_in_memory_assembly_delegate", (void**)delegate);
}

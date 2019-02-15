// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "comhost.h"
#include "fxr_resolver.h"
#include "pal.h"
#include "trace.h"
#include "error_codes.h"
#include "utils.h"

int get_com_activation_delegate(pal::string_t *app_path, com_activation_fn *delegate)
{
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
    pal::dll_t fxr;
    if (!pal::load_library(&fxr_path, &fxr))
    {
        trace::error(_X("The library %s was found, but loading it from %s failed"), LIBFXR_NAME, fxr_path.c_str());
        trace::error(_X("  - Installing .NET Core prerequisites might help resolve this problem."));
        trace::error(_X("     %s"), DOTNET_CORE_INSTALL_PREREQUISITES_URL);
        return StatusCode::CoreHostLibLoadFailure;
    }

    // Leak fxr

    auto get_com_delegate = (hostfxr_get_delegate_fn)pal::get_symbol(fxr, "hostfxr_get_com_activation_delegate");
    if (get_com_delegate == nullptr)
        return StatusCode::CoreHostEntryPointFailure;

    pal::string_t app_path_local{ host_path };

    // Strip the comhost suffix to get the 'app'
    size_t idx = app_path_local.rfind(_X(".comhost.dll"));
    assert(idx != pal::string_t::npos);
    app_path_local.replace(app_path_local.begin() + idx, app_path_local.end(), _X(".dll"));

    *app_path = std::move(app_path_local);

    auto set_error_writer_fn = (hostfxr_set_error_writer_fn)pal::get_symbol(fxr, "hostfxr_set_error_writer");
    propagate_error_writer_t propagate_error_writer_to_hostfxr(set_error_writer_fn);

    return get_com_delegate(host_path.c_str(), dotnet_root.c_str(), app_path->c_str(), (void**)delegate);
}

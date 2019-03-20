// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "nethost.h"
#include <error_codes.h>
#include <fxr_resolver.h>
#include <host_interface.h>
#include <pal.h>
#include <trace.h>
#include <utils.h>

NETHOST_API int STDMETHODCALLTYPE nethost_get_hostfxr_path(
    nethost_get_hostfxr_path_result_fn result,
    const char_t * assembly_path)
{
    host_mode_t mode = host_mode_t::invalid;
    pal::string_t root_path;
    if (assembly_path == nullptr)
    {
        mode = host_mode_t::muxer;
        if (!pal::get_own_executable_path(&root_path) || !pal::realpath(&root_path))
        {
            trace::error(_X("Failed to resolve full path of the current executable [%s]"), root_path.c_str());
            return StatusCode::CoreHostCurHostFindFailure;
        }
    }
    else
    {
        mode = host_mode_t::libhost;
        root_path = get_directory(assembly_path);
    }

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if(!fxr_resolver::try_get_path(mode, root_path, &dotnet_root, &fxr_path))
        return StatusCode::CoreHostLibMissingFailure;

    result(fxr_path.c_str());
    return StatusCode::Success;
}
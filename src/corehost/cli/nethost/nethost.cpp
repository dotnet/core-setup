// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "nethost.h"
#include <error_codes.h>
#include <fxr_resolver.h>
#include <pal.h>
#include <utils.h>

NETHOST_API int NETHOST_CALLTYPE nethost_get_hostfxr_path(
    nethost_get_hostfxr_path_result_fn result,
    const char_t * assembly_path)
{
    pal::string_t root_path;
    if (assembly_path != nullptr)
        root_path = get_directory(assembly_path);

    pal::string_t dotnet_root;
    pal::string_t fxr_path;
    if(!fxr_resolver::try_get_path(root_path, &dotnet_root, &fxr_path))
        return StatusCode::CoreHostLibMissingFailure;

    result(fxr_path.c_str());
    return StatusCode::Success;
}
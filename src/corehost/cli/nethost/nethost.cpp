// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "nethost.h"
#include <error_codes.h>
#include <pal.h>
#include <trace.h>

NETHOST_API int STDMETHODCALLTYPE nethost_get_hostfxr_path(
    nethost_get_hostfxr_path_result_fn result,
    const char_t * assembly_path)
{
    result(assembly_path != nullptr ? assembly_path : _X("<empty>"));
    return StatusCode::Success;
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "host_context.h"
#include <trace.h>

namespace
{
    const int32_t valid_host_context_marker = 0xabababab;
    const int32_t closed_host_context_marker = 0xcdcdcdcd;
}

host_context_t* host_context_t::from_handle(const hostfxr_handle handle, bool allow_invalid_type)
{
    if (handle == nullptr)
        return nullptr;
    
    host_context_t *context = static_cast<host_context_t*>(handle);
    int32_t marker = context->marker;
    if (marker == valid_host_context_marker)
    {
        if (allow_invalid_type || context->type != host_context_type::invalid)
            return context;
        
        trace::error(_X("Host context is in an invalid state"));
    }
    else if (marker == closed_host_context_marker)
    {
        trace::error(_X("Host context has already been closed"));
    }
    else
    {
        trace::error(_X("Invalid host context handle marker: 0x%x"), marker);
    }

    return nullptr;
}

host_context_t::host_context_t()
    : marker { valid_host_context_marker }
    , type { host_context_type::empty }
{ }

void host_context_t::close()
{
    marker = closed_host_context_marker;
}
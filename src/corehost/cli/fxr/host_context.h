// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __HOST_CONTEXT_H__
#define __HOST_CONTEXT_H__

#include <pal.h>

#include <corehost_context_contract.h>
#include "hostpolicy_resolver.h"

enum class host_context_type
{
    empty,        // Not populated, cannot be used for context-based operations
    initialized,  // Created, but not active (runtime not loaded)
    active,       // Runtime loaded for this context
    secondary,    // Created after runtime was loaded using another context
    invalid,      // Failed on loading runtime
};

struct host_context_t
{
    host_context_type type;

    hostpolicy_contract host_contract;
    corehost_context_contract context_contract;

    bool is_app;
    std::vector<pal::string_t> argv;

    std::unordered_map<pal::string_t, pal::string_t> config_properties;

    host_context_t()
        : type { host_context_type::empty }
    { }
};

#endif // __HOST_CONTEXT_H__
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __CONTEXT_CONTRACT_H__
#define __CONTEXT_CONTRACT_H__

#include <pal.h>

typedef void* context_handle;

enum class coreclr_delegate_type
{
    invalid,
    com_activation,
    load_in_memory_assembly,
    winrt_activation
};

struct corehost_context_contract
{
    size_t version;
    context_handle instance;
    int (*get_property_value)(
        const context_handle instance,
        const pal::char_t* key,
        const pal::char_t** value);
    int (*set_property_value)(
        const context_handle instance,
        const pal::char_t* key,
        const pal::char_t* value);
    int (*get_properties)(
        const context_handle instance,
        size_t *count,
        const pal::char_t** keys,
        const pal::char_t** values);
    int (*load_runtime)(
        const context_handle instance);
    int (*run_app)(
        const context_handle instance,
        const int argc,
        const pal::char_t* argv[]);
    int (*get_runtime_delegate)(
        const context_handle instance,
        coreclr_delegate_type type,
        void** delegate);
};

#endif // __CONTEXT_CONTRACT_H__
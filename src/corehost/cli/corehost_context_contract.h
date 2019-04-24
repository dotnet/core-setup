// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __COREHOST_CONTEXT_CONTRACT_H__
#define __COREHOST_CONTEXT_CONTRACT_H__

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
    context_handle handle;
    int (__cdecl *get_property_value)(
        const context_handle handle,
        const pal::char_t* key,
        /*out*/ const pal::char_t** value);
    int (__cdecl *set_property_value)(
        const context_handle handle,
        const pal::char_t* key,
        const pal::char_t* value);
    int (__cdecl *get_properties)(
        const context_handle handle,
        /*inout*/ size_t *count,
        /*out*/ const pal::char_t** keys,
        /*out*/ const pal::char_t** values);
    int (__cdecl *load_runtime)(
        const context_handle handle);
    int (__cdecl *run_app)(
        const context_handle handle,
        const int argc,
        const pal::char_t* argv[]);
    int (__cdecl *get_runtime_delegate)(
        const context_handle handle,
        coreclr_delegate_type type,
        /*out*/ void** delegate);
};

#endif // __COREHOST_CONTEXT_CONTRACT_H__
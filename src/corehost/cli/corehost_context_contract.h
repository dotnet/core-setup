// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __COREHOST_CONTEXT_CONTRACT_H__
#define __COREHOST_CONTEXT_CONTRACT_H__

#include <pal.h>

typedef void* context_handle;

enum intialization_options_t : int32_t
{
    none = 0x0,
    wait_for_initialized = 0x1,  // Wait until initialization through a differnt request is completed
};

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
    int (__cdecl *get_property_value)(
        const pal::char_t* key,
        /*out*/ const pal::char_t** value);
    int (__cdecl *set_property_value)(
        const pal::char_t* key,
        const pal::char_t* value);
    int (__cdecl *get_properties)(
        /*inout*/ size_t *count,
        /*out*/ const pal::char_t** keys,
        /*out*/ const pal::char_t** values);
    int (__cdecl *load_runtime)();
    int (__cdecl *run_app)(
        const int argc,
        const pal::char_t* argv[]);
    int (__cdecl *get_runtime_delegate)(
        coreclr_delegate_type type,
        /*out*/ void** delegate);
};

#endif // __COREHOST_CONTEXT_CONTRACT_H__
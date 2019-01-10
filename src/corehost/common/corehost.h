// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef _COREHOST_COMMON_COREHOST_H_
#define _COREHOST_COMMON_COREHOST_H_

//
// Type of delegate to retrieve from coreclr instance
//
enum coreclr_delegate_type
{
    // Delegate used to activate a managed COM server
    com_activation
};

#if FEATURE_LIBHOST

struct com_activation_context
{
    GUID class_id;
    GUID interface_id;
    const void *assembly_name;
    const void *type_name;
    void **class_factory_dest;
};

using com_activation_fn = int(*)(com_activation_context*);

int get_coreclr_delegate(
    coreclr_delegate_type type,
    void **delegate);

#endif

#endif //_COREHOST_COMMON_COREHOST_H_

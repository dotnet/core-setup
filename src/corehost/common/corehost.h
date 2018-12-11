// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef _COREHOST_COMMON_COREHOST_H_
#define _COREHOST_COMMON_COREHOST_H_

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

enum class coreclr_delegate_type
{
    com_activation
};

int get_coreclr_delegate(
    coreclr_delegate_type type,
    void **delegate);

#endif

#endif //_COREHOST_COMMON_COREHOST_H_

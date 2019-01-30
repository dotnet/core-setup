// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "executableheap.h"

// This file is a stop-gap measure until we have an allocator that manages an executable heap.

void* AllocateExecutable(std::size_t size)
{
    return nullptr;
}

void DeallocateExecutable(void* ptr)
{
    
}

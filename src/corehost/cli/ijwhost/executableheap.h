// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <new>

void* AllocateExecutable(std::size_t size);
void DeallocateExecutable(void* ptr);
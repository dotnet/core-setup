// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "pedecoder.h"

using DllMain_t = BOOL(STDMETHODCALLTYPE*)(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved);

bool PatchVTableEntriesForDLLAttach(PEDecoder& decoder);
void BootstrapThunkDLLDetach(PEDecoder& decoder);
bool AreThunksInstalledForModule(HMODULE instance);

using load_and_execute_in_memory_assembly_fn = int(*)(pal::dll_t handle, int argc, pal::char_t** argv);
using load_in_memory_assembly_fn = void(*)(pal::dll_t handle, const pal::char_t* path);

pal::hresult_t get_load_and_execute_in_memory_assembly_delegate(load_and_execute_in_memory_assembly_fn* delegate);
pal::hresult_t get_load_in_memory_assembly_delegate(pal::dll_t handle, load_in_memory_assembly_fn* delegate);

extern HANDLE g_heapHandle;
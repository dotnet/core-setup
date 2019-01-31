// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "pedecoder.h"

using DllMain_t = BOOL(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved);

bool PatchVTableEntriesForDLLAttach(PEDecoder& decoder);
void BootstrapThunkDLLDetach(PEDecoder& decoder);

pal::hresult_t LoadDllIntoRuntime(pal::dll_t hInstance);


HANDLE g_heapHandle;
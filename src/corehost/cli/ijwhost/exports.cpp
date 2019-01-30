// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal.h"
#include "pedecoder.h"
#include "ijwhost.h"
#include <cassert>

SHARED_API std::int32_t _CorExeMain()
{
    // Start runtime, execute user assembly
    return 0;
}

SHARED_API BOOL _CorDllMain(HINSTANCE hInst,
	DWORD  dwReason,
	LPVOID lpReserved
)
{
	BOOL res = TRUE;

	PEDecoder pe;

	if (!SUCCEEDED(pe.Init(hInst)))
	{
		return FALSE;
	}

	// In the following code, want to make sure that we do our own initialization before
	// we call into managed or unmanaged initialization code, and that we perform
	// uninitialization after we call into managed or unmanaged uninitialization code.
	// Thus, we do DLL_PROCESS_ATTACH work first, and DLL_PROCESS_DETACH work last.
	if (dwReason == DLL_PROCESS_ATTACH)
	{
		// If this is an invalid COR module, shouldn't be calling _CorDllMain
		if (!pe.CheckCORFormat())
		{
			return FALSE;
		}

		if (pe.IsILOnly() || (!pe.HasManagedEntryPoint() && !pe.HasNativeEntryPoint()))
		{
			// If there is no user entry point, then we don't want the
			// thread start/stop events going through because it slows down
			// thread creation operations
			DisableThreadLibraryCalls(hInst);
		}

		// Install the bootstrap thunks
		if (!PatchVTableEntriesForDLLAttach(pe))
		{
			return FALSE;
		}
	}

	// Now call the unmanaged entrypoint if it exists
	if (pe.HasNativeEntryPoint())
	{
		DllMain_t *pUnmanagedDllMain = (DllMain_t *)pe.GetNativeEntryPoint();
		assert(pUnmanagedDllMain != nullptr);
		res = (*pUnmanagedDllMain)(hInst, dwReason, lpReserved);
	}

	if (dwReason == DLL_PROCESS_DETACH)
	{
		BootstrapThunkDLLDetach(pe);
	}

	return res;
}

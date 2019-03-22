// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __NETHOST_H__
#define __NETHOST_H__

#if defined(_WIN32)
    #ifdef NETHOST_EXPORT
        #define NETHOST_API __declspec(dllexport)
    #else
        #define NETHOST_API __declspec(dllimport)
    #endif

    #define NETHOST_CALLTYPE __stdcall
    #ifdef _WCHAR_T_DEFINED
        using char_t = wchar_t;
    #else
        using char_t = unsigned short;
    #endif
#else
    #ifdef NETHOST_EXPORT
        #define NETHOST_API __attribute__((__visibility__("default")))
    #else
        #define NETHOST_API
    #endif

    #define NETHOST_CALLTYPE
    using char_t = char;
#endif

using nethost_get_hostfxr_path_result_fn = void(*)(const char_t * hostfxr_path);

//
// Get the path to the hostfxr library
//
// Parameters:
//   result
//     Callback invoked to return the hostfxr path. String passed in is valid for
//     the duration of the call.
//
//   assembly_path
//     Optional. Path to the compenent's assembly. Whether or not this is specified
//     determines the behaviour for locating the hostfxr library.
//     If nullptr, hostfxr is located using the enviroment variable or global registration
//     If specified, hostfxr is located as if the assembly_path is the apphost
//
// Return value:
//   0 on success, otherwise failure
//
extern "C" NETHOST_API int NETHOST_CALLTYPE nethost_get_hostfxr_path(
    nethost_get_hostfxr_path_result_fn result,
    const char_t * assembly_path);

#endif // __NETHOST_H__
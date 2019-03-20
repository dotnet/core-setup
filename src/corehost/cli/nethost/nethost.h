// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
//     If nullptr, hostfxr will be located as if the running executable is the muxer
//     If specified, hostfxr will be located as if the assembly_path is the apphost
//
// Return value:
//   0 on success, otherwise failure
//
extern "C" NETHOST_API int NETHOST_CALLTYPE nethost_get_hostfxr_path(
    nethost_get_hostfxr_path_result_fn result,
    const char_t * assembly_path);

#endif // __NETHOST_H__
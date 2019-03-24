// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __BDL_FILE_TYPE_H__
#define __BDL_FILE_TYPE_H__

#if FEATURE_APPHOST
#include <cstdint>

// FileType: Identifies the type of file embedded into the bundle.
// 
// The bundler differentiates a few kinds of files via the manifest,
// with respect to the way in which they'll be used by the runtime.
//
// Currently all files are extracted out to the disk, but future 
// implementations will process certain file_types directly from the bundle.

enum file_type_t : uint8_t
{
    assembly,
    ready2run,
    deps_json,
    runtime_config_json,
    extract,
    END
};


#endif // FEATURE_APPHOST

#endif // __BDL_FILE_TYPE_H__

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace Microsoft.DotNet.Build.Bundle
{
    // The bundler differentiates a few kinds of files via the manifest,
    // with respect to the way in which they'll be used by the runtime.
    //
    // - Runtime Configuration files: processed directly from memory
    // - Assemblies: loaded directly from memory.
    //               Currently, these are only pure-managed assemblies.
    //               Future versions should include R2R assemblies here.
    // - Other files (simply called "Files" below): files that will be 
    //               extracted out to disk. These include native binaries.

    public enum FileType
    {
        Application,       // Represents the main app, also an assembly
        Assembly,          // IL Assemblies, which will be processed from bundle
        DepsJson,          // Configuration file, processed from bundle
        RuntimeConfigJson, // Configuration file, processed from bundle
        PDB,               // PDB file, not bundled by default
        Other              // Files spilled to disk by the host
    };
}


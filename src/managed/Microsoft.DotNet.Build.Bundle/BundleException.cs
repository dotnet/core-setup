using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Linq;

namespace Microsoft.DotNet.Build.Bundle
{
    public class BundleException : Exception
    {
        public BundleException(string message) :
                base(message)
        {
        }
    }
}


using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.NET.HostModel.ComHost
{
    public class MissingGuidException : Exception
    {
        public MissingGuidException(string typeName)
        {
            TypeName = typeName;
        }

        public string TypeName { get; }
    }
}

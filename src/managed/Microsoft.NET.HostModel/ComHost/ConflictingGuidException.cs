using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.NET.HostModel.ComHost
{
    public class ConflictingGuidException : Exception
    {
        public ConflictingGuidException(string typeName1, string typeName2, Guid guid)
        {
            TypeName1 = typeName1;
            TypeName2 = typeName2;
            Guid = guid;
        }

        public string TypeName1 { get; }
        public string TypeName2 { get; }
        public Guid Guid { get; }
    }
}

using System;

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace PackageCompilationAssemblyResolverTest
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            
            var resolver = new PackageCompilationAssemblyResolver();
            var assemblies = new List<string>();

            var assembly = new CompilationLibrary(
                                "package",
                                "microsoft.csharp",
                                "4.3.0",
                                "null",
                                new string[] { Path.Combine("ref", "netstandard1.0", "Microsoft.CSharp.dll") },
                                new Dependency[] { },
                                true,
                                null,
                                null);
            if (resolver.TryResolveAssemblyPaths(assembly, assemblies))
            {
                Console.WriteLine("Succesfully resolved the assembly");
            }
            
        }
    }
}

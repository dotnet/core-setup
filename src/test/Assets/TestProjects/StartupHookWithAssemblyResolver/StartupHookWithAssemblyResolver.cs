using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

internal class StartupHook
{
    public static void Initialize()
    {
        AssemblyLoadContext.Default.Resolving += SharedHostPolicy.SharedAssemblyResolver.Resolve;
    }
}

namespace SharedHostPolicy
{
    public class SharedAssemblyResolver
    {
        public static Assembly Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (assemblyName.Name == "SharedLibrary")
            {
                string startupHookDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string sharedLibrary = Path.GetFullPath(Path.Combine(startupHookDirectory, "SharedLibrary.dll"));
                Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(sharedLibrary);
                if (assembly == null) {
                    throw new Exception("Unable to load SharedLibrary from startup hook");
                }
                return assembly;
            }
            throw new Exception("Resolve method called in startup hook for unexpected assembly " + assemblyName.Name);
        }
    }
}

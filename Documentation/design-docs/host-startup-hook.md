# Host startup hook

For .NET Core 3+, we want to provide a hook that allows managed code
to run before the main application's entry point.

## Motivation

This would allow hosting providers to put configuration and policy in
a single location, including settings that potentially influence load
behavior of the main entry point such as the AssemblyLoadContext
behavior. This could be used to set up callbacks for handling
Debug.Assert (if we make such an API available), or other
environment-dependent behavior. The hook needs to be separate from the
entry point, so that user code doesn't need to be modified.

## Proposed behavior

Environment variables or `<appname>.runtimeconfig.json` can be used to
specify a managed assembly and type that contains an `Initialize`
method.

```
DOTNET_MANAGED_HOST_ASSEMBLY=/path/to/ManagedHost.dll
DOTNET_MANAGED_HOST_TYPE=ManagedHostNamespace.ManagedHostType
```

```
{
    "runtimeOptions": {
        "managedHostAssembly": "/path/to/ManagedHost.dll",
        "managedHostType": "ManagedHostNamespace.ManagedHostType"
    }
}
```

The environment variables, if set, take precedence over the
`<appname>.runtimeconfig.json` settings. These settings result in
`ManagedHostType.Initialize()` being called in hostpolicy, before the
main assembly is loaded. If it read these settings from environment
variables, they will be inherited by child processes by default. It is
up to the ManagedHost.dll and user code to decide what to do about
this - ManagedHost.dll may clear them to prevent this behavior
globally, if desired.

Specifically, hostpolicy starts up coreclr and sets up a new AppDomain
with `ManagedHost.dll` on the TPA list. It then invokes
`ManagedHost.Initialize()`. This gives `ManagedHost` a chance to set
up new AssemblyLoadContexts, or register other callbacks. After
`Initialize()` returns, hostpolicy starts up the main entry point of
the app like usual.

Rather than forcing all configuration to be done through a single
predefined API, this creates a place where such configuration could be
centralized, while still allowing user code to do its own thing if it
so desires.

The producer of `ManagedHost.dll` needs to ensure that
`ManagedHost.dll` is compatible with the dependencies specified in the
main application's deps.json, since those dependencies are put on the
TPA list during the runtime startup, before `ManagedHost.dll` is
loaded. This means that `ManagedHost.dll` needs to built against the
same or lower version of .NET Core than the app.

## Example

This could be used with AssemblyLoadContext APIs to resolve
non-framework dependencies from a shared location, similar to the GAC
on full framework. Future changes to AssemblyLoadContext could make
this easier to use by making the default load context modifiable.

```
namespace SharedHostPolicy
{
    class SharedHostInitializer
    {
        public static void Initialize()
        {
            AssemblyLoadContext.Default.Resolving += SharedAssemblyResolver.LoadAssemblyFromSharedLocation;
        }
    }

    class SharedAssemblyResolver
    {
        public static Assembly LoadAssemblyFromSharedLocation(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string sharedAssemblyPath = // find assemblyName in shared location...
            if (sharedAssemblyPath != null)
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(sharedAssemblyPath)
            return null;
        }
    }
}
```
# Host startup hook

For .NET Core 3+, we want to provide a low-level hook that allows
injecting managed code to run before the main application's entry
point. This hook will make it possible for the host to customize the
behavior of managed applications during process launch, after they
have been deployed.

## Motivation

This would allow hosting providers to define custom configuration and
policy in managed code, including settings that potentially influence
load behavior of the main entry point such as the
`AssemblyLoadContext` behavior. The hook could be used to set up
tracing or telemetry injection, to set up callbacks for handling
Debug.Assert (if we make such an API available), or other
environment-dependent behavior. The hook is separate from the entry
point, so that user code doesn't need to be modified.

## Proposed behavior

The `DOTNET_STARTUP_HOOKS` environment variable can be used to specify
a list of managed assemblies and type names that contain `public
static void Initialize()` methods, which will be called in the order
specified, before the `Main` entry point:

```
DOTNET_STARTUP_HOOKS=/path/to/StartupHook1.dll!StartupHookNamespace.StartupHookType1;/path/to/StartupHook2.dll!StartupHookNamespace.StartupHookType2
```

This variable is a list of absolute assembly paths, each with a type
name following an exclamation mark. The list is delimited by the
platform-specific path separator (`;` on Windows and `:` on Unix). It
may not contain any empty entries or a trailing path separator.

Setting this environment variable will cause each of the specified
types' `public static void Initialize()` methods to be called in
order, synchronously, before the main assembly is loaded. The
environment variable will be inherited by child processes by
default. It is up to the `StartupHook.dll`s and user code to decide
what to do about this - `StartupHook.dll` may clear them to prevent
this behavior globally, if desired.

Specifically, hostpolicy starts up coreclr and sets up a new
AppDomain. It then invokes a private method in
`System.Private.CoreLib`, which will call each
`StartupHookType.Initialize()` in turn synchronously. This gives
`StartupHookType` a chance to set up new `AssemblyLoadContext`s, or
register other callbacks. After all of the `Initialize()` methods
return, control returns to hostpolicy, which then calls the main entry
point of the app like usual.

Rather than forcing all configuration to be done through a single
predefined API, this creates a place where such configuration could be
centralized, while still allowing user code to do its own thing if it
so desires.

The producer of `StartupHook.dll` needs to ensure that
`StartupHook.dll` is compatible with the dependencies specified in the
main application's deps.json, since those dependencies are put on the
TPA list during the runtime startup, before `StartupHook.dll` is
loaded. This means that `StartupHook.dll` needs to built against the
same or lower version of .NET Core than the app.

## Example

This could be used with `AssemblyLoadContext` APIs to resolve
dependencies not on the TPA list from a shared location, similar to
the GAC on full framework. It could also be used to forcibly preload
assemblies that are on the TPA list from a different location. Future
changes to `AssemblyLoadContext` could make this easier to use by
making the default load context or TPA list modifiable.

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
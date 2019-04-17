# Native hosting - Load assembly

Native hosting is the ability to host the .NET Core runtime in an arbitrary process, one which didn't start from .NET Core produced binaries. This document focuses on the ability to load an assembly from the native app without a need to run managed application.

For a general discussion on native hosting, please see [Native hosting](native-hosting.md)

*Note: In this document "native app" refers to an application/component which is not running on the .NET Core runtime. This can be a true native app written in C++ or similar, but it can also be any other technology which is not .NET Core.*


## Scenarios
* **Hosting managed components**  
Native app which wants to load managed assembly and call into it for some functionality. Must support loading multiple such components side by side.


## Existing support
[COM Activation](COM-activation.md) allows native apps to effectively load managed components (assemblies), but it requires the use of COM activation APIs and general COM related registration and setup. This is Windows only.
[WinRT Activation](WinRT-activation.md) also allows native apps to effectively load managed components, but it's tied to WinRT and only supports loading WinRT components (`.winmd`). This is Windows only.


## High-level proposal
In .NET Core 3.0 the hosting layer (see [here](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-components.md)) ships with several hosts. These are binaries which act as the entry points to the .NET Core hosting/runtime:
* The "muxer" (`dotnet.exe`)
* The `apphost` (`.exe` which is part of the app)
* The `comhost` (`.dll` which is part of the app and acts as COM server) - Windows only
* The `ijwhost` (`.dll` consumed via `.lib` used by IJW assemblies) - Windows only
* The `winrthost` (`.dll` which is part of the app and acts as WinRT server) - Windows only

Every one of these hosts serve different scenario and expose different APIs. The one thing they have in common is that their main purpose is to find the right `hostfxr`, load it and call into it to execute the desired scenario. For the most part all these hosts are basically just wrappers around functionality provided by `hostfxr`.

The proposal is to add a new host library `nethost` which can be used by native apps to easily host managed components.

*Technical note: All strings in the proposed APIs are using the `char_t` in this document for simplicity. In real implementation they are of the type `pal::char_t`. In particular:*
* *On Windows - they are `WCHAR *` using `UTF16` encoding*
* *On Linux/macOS - they are `char *` using `UTF8` encoding*


### Load managed component and get a function pointer
This new functionality would let native app load a .NET Core component (and its assemblies) into a process and get a function pointer to a managed method from the component.

Input for this process is:
* `assembly_path` - the path to the main assembly of the component to load.
* `type_name` - the fully qualified type name from which to get a method.
* `method_name` - the name of the method to get from the type and for which to return a function pointer.

Process to load managed component
* Create a new isolated [`AssemblyLoadContext`](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext?view=netcore-3.0) (possibly reusing ALCs to avoid loading the same assembly multiple times) using the [`AssemblyDependencyResolver`](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblydependencyresolver?view=netcore-3.0) with the component's assembly specified by `assembly_path` to provide dependency resolution.
* Load the component's main assembly specified by `assembly_path` into it
* Find the requested `type_name` and `method_name` using reflection
  * The requested method must be static and there can be only one match (no support for overloading)
* Return a native callable function pointer to the requested method


## Support loading managed components in `hostfxr`
The low-level APIs for loading managed components are available in `hostfxr`. These provide full control to the native app over the process of loading the CoreCLR and enable loading managed components.

This will be implemented by extending the `hostfxr_get_runtime_delegate` and its `hostfxr_delegate_type` by adding a new enum value `load_assembly_and_get_method`.

The return function pointer to a runtime helper will look like this (the name of the function is not important as it's just a function pointer:
``` C
using load_assembly_and_get_method_fn = int (*)(
    const char_t * assembly_path,
    const char_t * type_name,
    const char_t * method_name,
    void * reserved,
    void ** function_pointer
);
```
* `assembly_path` - path to the main assembly of the component (the one with `.deps.json` next to it) to load.
* `type_name` - the name of the type to get the method from. The implementation will probably use something like `Assembly.GetType` so the syntax of the type name should be the same as for that API.
* `method_name` - the name of the method to return. This must be a static method with only one overload.
* `reserved` - currently unused and must be `nullptr`. Extensibility point for the future - for example we could eventually support unloadability through this.
* `function_pointer` - the returned function pointer.

The runtime helper implements the "Process to load managed component" as described above and returns a function pointer to the requested method.

It should be possible to use the runtime helper multiple times to get function pointers to several methods from various types.
The runtime helper should be thread safe, that is can be called from multiple threads at the same time.

If the runtime is initialized by this function, the default load context will only be populated with framework assemblies (via TPA), none of the component's assemblies will be loaded into the default context.

*As proposed there would be no support for unloading components. For discussion on possible solutions see open issues below.*

### Open questions
* What type of strings are going to be passed in the `assembly_path`, `type_name` and `method_name`. Should it be `char_t` which would mean `WCHAR` on Windows and `char` on Linux/macOS, or should it be the same for all platforms, so probably UTF8 `char *`? It would be easier for the runtime to take only one type across all platforms. On the other hand the native app would probably prefer the platform specific strings. Especially UTF8 on Windows is relatively tricky to work with in native code.
* Do we require the method to be `public`? Or do we allow `internal` and/or `private` as well?
* How exactly do we perform marshaling of parameters for the method? Do we limit method parameters somehow (only primitive types for example)? Can we rely on marshaling attributes and so on? What can interop do and how is it usable?
* Do we somehow try to limit the types? For example disallow generics? (no real technical reason I can think of... but maybe)
* Details of error reporting - do we use `HRESULT` or something else? Do we design some additional way to pass detailed error information?
* How do we define the behavior if the runtime helper is used to load multiple different assemblies? The framework and such is all set in stone, so this is equivalent to using `AssemblyDependencyResolver` and `AssemlyLoadContext.LoadFromAssemblyPath` from managed code. Do we allow this? Describe behavior?

### Sample usage
The native app might then follow this process:
* Potentially use `nethost` and its `get_hostfxr_path` to locate and load `hostfxr`.
* Call `hostfxr_initialize_for_runtime_config` providing the path to the `.runtimeconfig.json` of the component to load.
* Potentially use other methods on the `hostfxr` to inspect and modify the initialization of the runtime (for example `hostfxr_get_runtime_property`, `hostfxr_set_runtime_property` and so on).
* Call `hostfxr_get_runtime_delegate` with the `hostfxr_delegate_type::load_assembly_and_get_function_pointer` and getting back a function pointer to the runtime helper of the `load_assembly_and_get_method_fn` type - let's call the returned runtime helper `pfn_load_assembly_and_get_method`.
* Call the returned runtime helper `pfn_load_assembly_and_get_method` passing the `assembly_path`, `type_name` and `method_name` and getting back a function pointer for the managed method.
* Closing the host context via `hostfxr_close`.



## New host binary for component hosting
Add new library `nethost` which will act as the easy to use host for loading managed components.
The library would be a dynamically loaded library (`.dll`, `.so`, `.dylib`). For ease of use there would be a header file for C++ apps as well as `.lib`/`.a` for easy linking.
Apps using the component hosting functionality would ship this library as part of the app. Unlike the `apphost`, `comhost`, `ijwhost` and `winrthost`, the `nethost` will not be directly supported by the .NET Core SDK since its target usage is not from .NET Core apps.

The exact delivery mechanism is TBD (pending investigation), but it will include plain `.zip` (for any consumer) and potentially NuGet. The binary itself should be signed by Microsoft as there will be no support for modifying the binary as part of custom application build (unlike `apphost` or `comhost`).

### Load managed component and get a function pointer
``` C++
int load_assembly_and_get_method(
        const char_t * assembly_path,
        const char_t * type_name,
        const char_t * method_name,
        const void * reserved,
        void ** function_pointer);
)
```
This API will
* Locate a `.runtimeconfig.json` by looking next to the assembly specified in `assembly_path` and determine the frameworks it requires to run. (Note that only framework dependent components will be supported for now).
* If the process doesn't have CoreCLR loaded (more specifically `hostpolicy` library)
  * Using the `.runtimeconfig.json` resolve required frameworks (just like if running an application) and load the runtime.
* Else the CoreCLR is already loaded, in that case validate that required frameworks for the component can be satisfied by the runtime.
  * If the required frameworks are not already present, fail. No support to load additional frameworks for now.
* Follow the "Process to load managed component" described above to get a native callable function pointer for the method specified by `assembly_path`, `type_name` and `method_name`.

The `reserved` argument is currently not used and must be set to `nullptr`. It is present to make this API extensible. In a future version we may need to add more parameters to this call in which case this parameter would be a pointer to a `struct` with the additional fields.

If the runtime is initialized by this function, the default load context will only be populated with framework assemblies (via TPA), none of the component's assemblies will be loaded into the default context.

*As proposed there would be no support for unloading components. For discussion on possible solutions see open issues below.*

Similar open questions as those described in the `hostfxr` API above apply.


## Impact on hosting components

### `hostpolicy`
Impact on `hostpolicy` API is minimal:
* Add a new value to the `coreclr_delegate_type` and the respective managed method in `System.Private.CoreLib`.


# Open issues
* Support unloading of managed components  
Currently there's no way to unload the runtime itself (and we don't have any plans to add this ability). That said, managed components will be loaded into isolated ALCs and thus the ALC unload feature can be used to unload the managed component (leaving the runtime still active in the process).
  * How to expose this functionality - how to identify which component to unload (assembly name, one of the returned function pointers)?
  * What happens with the returned function pointers - unloading the underlying code would lead to these function pointers to become invalid, likely causing undefined behavior when used.
  * How to handle sharing - for performance and functionality reasons it would make a lot of sense to not load the same assembly twice - basically if the component loading API is used twice on the same assembly, the assembly is loaded only once and two separate function pointers are returned. Unloading may introduce unexpected failure modes.
* Future interop considerations
  * `NativeCallableAttribute` (DLL_EXPORT implemented in managed) - we believe that on Windows this would be close to `ijwhost` and on Linux/macOS we should be able to create something which uses `nethost`.
  * The proposed `nethost` API only supports calling static managed methods. If the use case requires exposing objects/interfaces some amount of infra work is needed to expose those on top of the proposed API.

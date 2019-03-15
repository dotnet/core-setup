# Native hosting - Load assembly

Native hosting is the ability to host the .NET Core runtime in an arbitrary process, one which didn't start from .NET Core produced binaries. This document focuses on the ability to load an assembly from the native app without a need to run managed application.

For a general discussion on native hosting, please see [Native hosting](native-hosting.md)

*Note: In this document "native app" refers to an application/component which is not running on the .NET Core runtime. This can be a true native app written in C++ or similar, but it can also be any other technology which is not .NET Core.*


## Scenarios
* **Hosting managed components**  
Native app which wants to load managed assembly and call into it for some functionality. Must support loading multiple such components side by side.


## Existing support
[COM Activation](COM-activation.md) allows native apps to effectively load managed components (assemblies), but it requires the use of COM activation APIs and general COM related registration and setup.


## High-level proposal
In .NET Core 3.0 the hosting layer (see [here](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-components.md)) ships with several hosts. These are binaries which act as the entry points to the .NET Core hosting/runtime:
* The "muxer" (`dotnet.exe`)
* The `apphost` (`.exe` which is part of the app)
* The `comhost` (`.dll` which is part of the app and acts as COM server)
* The `ijwhost` (`.dll` consumed via `.lib` used by IJW assemblies)

Every one of these hosts serve different scenario and expose different APIs. The one thing they have in common is that their main purpose is to find the right `hostfxr`, load it and call into it to execute the desired scenario. For the most part all these hosts are basically just wrappers around functionality provided by `hostfxr`.

The proposal is to add a new host library `nethost` which can be used by native apps to easily host managed components.

*Technical note: All strings in the proposed APIs are using the `char_t` in this document for simplicity. In real implementation they are of the type `pal::char_t`. In particular:*
* *On Windows - they are `WCHAR *` using `UTF16` encoding*
* *On Linux/macOS - they are `char *` using `UTF8` encoding*


## New host binary for component hosting
Add new library `nethost` which will act as the easy to use host for loading managed components.
The library would be a dynamically loaded library (`.dll`, `.so`, `.dylib`). For ease of use there would be a header file for C++ apps as well as `.lib`/`.a` for easy linking.
Apps using the component hosting functionality would ship this library as part of the app. Unlike the `apphost`, `comhost` and `ijwhost`, the `nethost` will not be directly supported by the .NET Core SDK since it's target usage is not from .NET Core apps.

The exact delivery mechanism is TBD (pending investigation), but it probably should include NuGet (for C++ projects) and plain `.zip` (for any consumer). The binary itself should be signed by Microsoft as there will be no support for modifying the binary as part of custom application build (unlike `apphost`).

### Load managed component and get a function pointer
``` C++
int nethost_load_assembly_method(
        const char_t * assembly_path,
        const char_t * type_name,
        const char_t * method_name,
        const void * reserved,
        void ** delegate);
)
```
This API will
* Locate the assembly using the `assembly_path` and its `.runtimeconfig.json` and determine the frameworks it requires to run. (Note that only framework dependent components will be supported for now).
* If the process doesn't have CoreCLR loaded (more specifically `hostpolicy` library)
  * Using the `.runtimeconfig.json` resolve required frameworks (just like if running an application) and load the runtime.
* Else the CoreCLR is already loaded, in that case validate that required frameworks for the component can be satisfied by the runtime.
  * If the required frameworks are not already present, fail. No support to load additional frameworks for now.
* Call into the runtime (`System.Private.CoreLib` specifically)
  * Create a new isolated `AssemblyLoadContext` (possibly reusing for the same components) using the `AssemblyDependencyResolver` with the component's assembly to provide dependency resolution
  * Load the component's assembly into it
  * Find the requested `type_name` and `method_name`
  * Return a native callable function pointer to the requested method

The `reserved` argument is currently not used and must be set to `nullptr`. It is present to make this API extensible. In a future version we may need to add more parameters to this call in which case this parameter would be a pointer to a `struct` with the additional fields.

If the runtime is initialized by this function, it will only be populated with framework assemblies (its TPA), none of the component's assemblies will be loaded into the default context.

*As proposed there would be no support for unloading components. For discussion on possible solutions see open issues below.*


## Impact on hosting components

### `hostfxr`
Extend the `hostfxr_delegate_type` (soon to be introduced with the `ijwhost`) to add the new runtime entry point in `System.Private.CoreLib` - name is TBD.

### `hostpolicy`
Impact on `hostpolicy` API is minimal:
* Implementation of the `nethost_load_assembly_method` will just add a new value to the `coreclr_delegate_type` (soon to be introduced with the `ijwhost`) and the respective managed method in `System.Private.CoreLib`.

# Open issues
* Support unloading of managed components  
Currently there's no way to unload the runtime itself (and we don't have any plans to add this ability). That said, managed components will be loaded into isolated ALCs and thus the ALC unload feature can be used to unload the managed component (leaving the runtime still active in the process).
  * How to expose this functionality - how to identify which component to unload (assembly name, one of the returned function pointers)?
  * What happens with the returned function pointers - unloading the underlying code would lead to these function pointers to become invalid, likely causing undefined behavior when used.
  * How to handle sharing - for performance and functionality reasons it would make a lot of sense to not load the same assembly twice - basically if the component loading API is used twice on the same assembly, the assembly is loaded only once and two separate function pointers are returned. Unloading may introduce unexpected failure modes.
* Future interop considerations
  * .NET Core WinRT components - we believe that this is very similar to `comhost` and thus should work with the current design
  * `NativeCallableAttribute` (DLL_EXPORT implemented in managed) - we believe that on Windows this would be close to `ijwhost` and on Linux/macOS we should be able to create something which uses `nethost`.
  * The proposed `nethost` API only supports calling static managed methods. If the use case requires exposing objects/interfaces some amount of infra work is needed to expose those on top of the proposed API.

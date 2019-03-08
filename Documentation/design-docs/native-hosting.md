# Native hosting

Native hosting is the ability to host the .NET Core runtime in an arbitrary process, one which didn't start from .NET Core produced binaries.

*Note: In this document "native app" refers to an application/component which is not running on the .NET Core runtime. This can be a true native app written in C++ or similar, but it can also be any other technology which is not .NET Core.*


## Scenarios
* **Hosting managed components**  
Native app which wants to load managed assembly and call into it for some functionality. Must support loading multiple such components side by side.
* **Hosting managed apps**  
Native app which wants to run a managed app in-proc. Basically a different implementation of the existing .NET Core hosts (`dotnet.exe` or `apphost`). The intent is the ability to modify how the runtime starts and how the managed app is executed (and where it starts from).
* **App using other .NET Core hosting services**  
App (native or .NET Core both) which needs to use some of the other services provided by the .NET Core hosting layer. For example the ability to locate available SDKs and so on.


## Existing support
* C-style ABI in `coreclr`  
`coreclr` exposes ABI to host the .NET Core runtime and run managed code already using C-style APIs. See this [header file](https://github.com/dotnet/coreclr/blob/master/src/coreclr/hosts/inc/coreclrhost.h) for the exposed functions.
This API requires the native app to locate the runtime and to fully specify all startup parameters for the runtime. There's no inherent interoperability between these APIs and the .NET Core SDK.
* COM-style ABI in `coreclr`  
`coreclr` exposes COM-style ABI to host the .NET Core runtime and perform a wide range of operations on it. See this [header file](https://github.com/dotnet/coreclr/blob/master/src/pal/prebuilt/inc/mscoree.h) for more details.
Similarly to the C-style ABI the COM-style ABI also requires the native app to locate the runtime and to fully specify all startup parameters.
There's no inherent interoperability between these APIs and the .NET Core SDK.
* `hostfxr` and `hostpolicy` APIs
The hosting layer of .NET Core already exposes some functionality as C-style ABI on either the `hostfxr` or `hostpolicy` libraries. These can execute application, determine available SDKs, determine native dependency locations, resolve component dependencies and so on.
Unlike the above `coreclr` based APIs these don't require the caller to fully specify all startup parameters, instead these APIs understand artifacts produced by .NET Core SDK making it much easier to consume SDK produced apps/libraries.
The native app is still required to locate the `hostfxr` or `hostpolicy` libraries. These APIs are also designed for specific narrow scenarios, any usage outside of these bounds is typically not possible.


## Scope
This document focuses on easy-to-use hosting which cooperates with the .NET Core SDK and consumes the artifacts produced by building the managed app/libraries directly. It completely ignores the COM-style ABI as it's hard to use from some programming languages.

As such the document explicitly excludes any hosting based on directly loading `coreclr`. The document focuses on using the existing .NET Core hosting layer in new ways. For details on the .NET Core hosting components see [this document](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-components.md).


## High-level proposal
In .NET Core 3.0 the hosting layer (see [here](https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-components.md)) ships with several hosts. These are binaries which act as the entry points to the .NET Core hosting/runtime:
* The "muxer" (`dotnet.exe`)
* The `apphost` (`.exe` which is part of the app)
* The `comhost` (`.dll` which is part of the app and acts as COM server)
* The `ijwhost` (`.dll` consumed via `.lib` used by IJW assemblies)

Every one of these hosts serve different scenario and expose different APIs. The one thing they have in common is that their main purpose is to find the right `hostfxr`, load it and call into it to execute the desired scenario. For the most part all these hosts are basically just wrappers around functionality provided by `hostfxr`.

The proposal is to add a new host library `nethost` which can be used by native apps to easily host managed components and to easily locate `hostfxr` for more advanced scenarios.

At the same time add the ability to pass additional runtime properties when starting the runtime through the hosting entry points (starting app, loading component). This can be used by the native app to:
* Register startup hook without modifying environment variables (which are inherited by child processes)
* Introduce new runtime knobs which are only available for native hosts without the need to update the hosting APIs every time.

This new ability would be added to both `hostfxr` and to the `nethost`.


*Technical note: All strings in the proposed APIs are using the `char_t` in this document for simplicity. In real implementation they are of the type `pal::char_t`. In particular:*
* *On Windows - they are `WCHAR *` using `UTF16` encoding*
* *On Linux/macOS - they are `char *` using `UTF8` encoding*


## New host binary for component hosting
Add new library `nethost` which will act as the easy to use host for loading managed components.
The library would be a dynamically loaded library (`.dll`, `.so`, `.dylib`). For ease of use there would be a header file for C++ apps as well as `.lib`/`.a` for easy linking.
Apps using the component hosting functionality would ship this library as part of the app. So similarly to `apphost`/`comhost`/`ijwhost` the `nethost` will be shipped as part of .NET Core SDK to be included in customer's projects.

This library would expose two APIs

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
  * Create a new isolated `AssemblyLoadContext` (possibly reusing for the same components)
  * Load the component's assembly into it
  * Find the requested `type_name` and `method_name`
  * Return a native callable function pointer to the requested method

The `reserved` argument is currently not used and must be set to `nullptr`. It is present to make this API extensible. In a future version we may need to add more parameters to this call in which case this parameter would be a pointer to a `struct` with the additional fields.

If the runtime is initialized by this function, it will only be populated with framework assemblies (its TPA), none of the component's assemblies will be loaded into the default context.

*As proposed there would be no support for unloading components. For discussion on possible solutions see open issues below.*

### Locate `hostfxr`
``` C++
typedef void(* nethost_get_hosfxr_path_result_fn)(const char_t * hostfxr_path);

int nethost_get_hostfxr_path(
        nethost_get_hostfxr_path_result_fn result,
        const_t char * assembly_path); // Optional
```

This API locates the `hostfxr` and returns its path by calling the `result` function. (This callback design is chosen so that it's clear and easy to define memory ownership.)

`assembly_path` is optional:
* If `nullptr` the "muxer" (`dotnet.exe`) behavior is used to locate the `hostfxr`.
* If specified the `apphost` behavior is used.


## Improve API to run application
New API will be added to `hostfxr`.

``` C++
struct hostfxr_runtime_property
{
    const char_t * key;
    const char_t * value;
};

struct hostfxr_parameters
{
    size_t size;
    const char_t * host_path;
    const char_t * dotnet_root;
    const char_t * app_path;
    size_t additional_properties_count;
    const hostfxr_runtime_property * additional_properties;
};

int hostfxr_main_with_parameters(
        const int argc,
        const char_t * argv[],
        const hostfxr_parameters * parameters);
```

This new API supercedes the existing `hostfxr_main` and `hostfxr_main_startupinfo`. The function is the main entry point for running applications and dotnet commands. It encapsulates both the `muxer` functionality for running apps via `dotnet app.dll` or `dotnet exec app.dll`, as well as running SDK commands like `dotnet new` and so on.

The proposed behavior is the same as for the existing entry points `hostfxr_main` and `hostfxr_main_startupinfo` with the addition of `parameters` structure. This is meant to:
* Add the ability to specify additional runtime properties which will be added to the property bag pass to the runtime during its initialization.
* Make the API extensible without adding new entry point. In the future the parameters structure can be extended with additional members. The `hostfxr_parameters` structure is versioned by its `size` field which must be set by the caller to `sizeof(hostfxr_parameters)`. If we rev it in the future, we would introduce a new type, for example `hostfxr_parameters_extended` which would add new fields and thus increase the size.

The ability to specify additional runtime properties introduces potential collisions with runtime properties initialized by the hosting layer. This ability is not new, it's already possible to specify arbitrary set of additional runtime properties through the `configProperties` section of the `.runtimeconfig.json`. The existing behavior for collisions is somewhat undefined by it effectively means:
* Properties supplied by the host are added first
* Any additional properties are added after these - duplicates are retained
* Runtime initialization will use the last value of any given property it finds for properties it needs to start running managed code
* Later on during runtime initialization duplicates will cause a failure and the runtime will fail to start with `0x80070057 (E_INVALIDARG)` (internally the failure is due to adding the same key twice to a `Dictionary<K,V>`).

This means that having duplicate properties leads to runtime initialization failure (although the exact set of properties might end up with different failure modes, but it will always fail).

For now, this proposal is not trying to improve this behavior and the additional properties specified through the new `hostfxr` API will behave exactly like those specified via `.runtimeconfig.json`. Since duplicates are not allowed (they cause failures) there's no need to specify precedence (although the logical one would be to take the properties from the API over those in `.runtimeconfig.json`).

## Impact on hosting components

### `hostfxr`
Aside from the new API `hostfxr_main_with_parameters` the only other improvement is to extend the existing `hostfxr_get_runtime_delegate` to accept additional properties:
``` C++
int hostfxr_get_runtime_delegate(
        const hostfxr_parameters * parameters,
        hostfxr_delegate_type type,
        void **delegate);
```

The functionality of the entry point will remain as is, the only addition is using extensible parameter structure and the ability to pass additional runtime properties.

### `hostpolicy`
Impact on `hostpolicy` API is minimal:
* Passing additional properties is already implemented as that is the mechanism used to pass properties defined in `.runtimeconfig.json`. So `hostfxr` would just merge the properties from the API with those from `.runtimeconfig.json` and use existing mechanism to pass it to `hostpolicy`.
* Implementation of the `nethost_load_assembly_method` will just add a new value to the `coreclr_delegate_type` (soon to be introduced with the `ijwhost`) and the respective managed method in `System.Private.CoreLib`.

# Open issues
* Support unloading of managed components  
Currently there's no way to unload the runtime itself (and we don't have any plans to add this ability). That said, managed components will be loaded into isolated ALCs and thus the ALC unload feature can be used to unload the managed component (leaving the runtime still active in the process).
  * How to expose this functionality - how to identify which component to unload (assembly name, one of the returned function pointers)?
  * What happens with the returned function pointers - unloading the underlying code would lead to these function pointers to become invalid, likely causing undefined behavior when used.
  * How to handle sharing - for performance and functionality reasons it would make a lot of sense to not load the same assembly twice - basically if the component loading API is used twice on the same assembly, the assembly is loaded only once and two separate function pointers are returned. Unloading may introduce unexpected failure modes.
* Maybe add `apphost_get_hostfxr_path` on the existing `apphost` - this is to make it even easier to implement custom hosting for entire managed app as the custom host would not need to carry a `nethost` and would get a 100% compatible behavior by using the same `apphost` as the app itself.
* Future interop considerations
  * .NET Core WinRT components - we believe that this is very similar to `comhost` and thus should work with the current design
  * `NativeCallableAttribute` (DLL_EXPORT implemented in managed) - we believe that on Windows this would be close to `ijwhost` and on Linux/macOS we should be able to create something which uses `nethost`.
  * The proposed `nethost` API only supports calling static managed methods. If the use case requires exposing objects/interfaces some amount of infra work is needed to expose those on top of the proposed API.

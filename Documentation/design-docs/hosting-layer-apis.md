# Hosting layer APIs

Functionality for advanced hosting scenarios is exposed on the `hostfxr` and `hostpolicy` libraries through C-style APIs.

The `char_t` strings in the below API descriptions are defined based on the platform:
* Windows     - UTF-16 (2-byte `wchar_t`)
  * Note that `wchar_t` is defined as a [native type](https://docs.microsoft.com/cpp/build/reference/zc-wchar-t-wchar-t-is-native-type), which is the default in Visual Studio.
* Unix        - UTF-8  (1-byte `char`)

## Host FXR

All exported functions and function pointers in the `hostfxr` library use the `__cdecl` calling convention on the x86 platform.

## Host Policy

All exported functions and function pointers in the `hostpolicy` library use the `__cdecl` calling convention on the x86 platform.

### .NET Core 1.0+

``` C
int corehost_load(host_interface_t *init)
```

Initialize `hostpolicy`. This stores information that will be required to do all the processing necessary to start CoreCLR, but it does not actually do any of that processing.
* `init` - structure defining how the library should be initialized

If already initalized, this function returns success without reinitializing (`init` is ignored).

``` C
int corehost_main(const int argc, const char_t* argv[])
```

Run an application.
* `argc` / `argv` - command-line arguments

This function does not return until the application completes execution. It will shutdown CoreCLR after the application executes.

``` C
int corehost_unload()
```

Uninitialize `hostpolicy`.

### .NET Core 2.1+
``` C
int corehost_main_with_output_buffer(
    const int argc,
    const char_t *argv[],
    char_t buffer[],
    int32_t buffer_size,
    int32_t *required_buffer_size)
```

Run a host command and return the output. `corehost_load` should have been called with the `host_command` set on the `host_interface_t`. This function operates in the hosting layer and does not actually run CoreCLR.
* `argc` / `argv` - command-line arguments
* `buffer` - buffer to populate with the output (including a null terminator)
* `buffer_size` - size of `buffer` in `char_t` units
* `required_buffer_size` - if `buffer` is too small, this will be populated with the minimum required buffer size

### .NET Core 3.0+

``` C
int corehost_get_coreclr_delegate(coreclr_delegate_type type, void **delegate)
```

Get a delegate for CoreCLR functionality
* `type` - requested type of runtime functionality
* `delegate` - function pointer to the requested runtime functionality

``` C
typedef void(*corehost_resolve_component_dependencies_result_fn)(
    const char_t *assembly_paths,
    const char_t *native_search_paths,
    const char_t *resource_search_paths);

int corehost_resolve_component_dependencies(
    const char_t *component_main_assembly_path,
    corehost_resolve_component_dependencies_result_fn result)
```

Resolve dependencies for the specified component.
* `component_main_assembly_path` - path to the component
* `result` - callback which will receive the results of the component dependency resolution

See [Component dependency resolution support in host](host-component-dependencies-resolution.md)

``` C
typedef void(*corehost_error_writer_fn)(const char_t *message);

corehost_error_writer_fn corehost_set_error_writer(corehost_error_writer_fn error_writer)
```

Set a callback which will be used to report error messages. By default no callback is registered and the errors are written to standard error.
* `error_writer` - callback function which will be invoked every time an error is reported. When set to `nullptr`, this function unregisters any previously registered callback and the default behaviour is restored.

The return value is the previouly registered callback (which is now unregistered) or `nullptr` if there was no previously registered callback.

The error writer is registered per-thread. On each thread, only one callback can be registered. Subsequent registrations overwrite the previous ones.

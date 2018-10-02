# Component dependency resolution support in host

This feature provides support for component dependency resolution. The usage is dynamic assembly loading by the runtime. In .NET Core components typically use `.deps.json` to describe their dependencies. So dynamic loading needs to be able to parse and use the information in `.deps.json` to resolve dependencies of components.

The host components (mostly `hostpolicy`) already contain code which does `.deps.json` parsing and resolution for the app start, so to avoid code duplication and maintain consistent behavior the same code is used for component dependency resolution as well.

## Notes
* The component dependency resolution uses the same servicing and shared store paths as the app. So it's up to the app to set these up either via config or environment variables.

## Open questions
* What probing paths (as per [host-probing](host-probing.md)) should be used for the component. Currently it's the same set as for the app. Which means that scenarios like self-contained app consuming `dotnet build` produced component won't work since self-contained apps typically have no probing paths (specifically they don't setup NuGet paths in any way).
* What other settings from the app should be reused by the component
  * probe_paths - see above
  * tfm - reused
  * host_mode - reused
  * RID fallback graph from the root framework - reused
* What environment variables should be used
  * `DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX` - used just like in the app
  * `ProgramFiles` and `ProgramFiles(x86")` - used to find servicing root and the shared store
  * `DOTNET_SHARED_STORE` - used to find the shared store - just like the app
  * `DOTNET_MULTILEVEL_LOOKUP` - used to enable multi-level lookup - used just like in the app
* Currently SDK doesn't seem to generate `.runtimeconfig.json` or `.runtimeconfig.dev.json` for components - ever. Is this really the case? Do we need to support this? What values would we take from these files?
* Do we need to write breadcrumbs when doing the dependency resolution for components?
* Currently we don't consider frameworks for the app when computing probing paths for resolving assets from the component's `.deps.json`. This is a different behavior from the app startup where these are considered. Is it important - needed?
* Error reporting: If the native code fails currently it only returns error code. So next to no information. It writes detailed error message into the stderr (even with tracing disabled) which is also wrong since it pollutes the process output. It should be captured and returned to the caller, so that the managed code can include it in the exception. There's also consideration for localization - as currently the native components don't use localized error reporting.
* Add ability to corelate tracing with the runtime - probably some kind of activity ID
* Handling of native assets - currently returning just probing paths. Would be cleaner to return full resolved paths. But we would have to keep some probing paths. In the case of missing `.deps.json` the native library should be looked for in the component directory - thus requires probing - we can't figure out which of the files in the folder are native libraries in the hosts.
* Handling of satellite assemblies (resource assets) - currently returning just probing paths which exclude the culture. So from a resolved asset `./foo/en-us/resource.dll` we only take `./foo` as the probing path. Consider using full paths instead - probably would require more parsing as we would have to be able to figure out the culture ID somewhere to build the true map AssemblyName->path in the managed class.
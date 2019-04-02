# Breaking changes

### Roll on patches only never rolls from release to pre-release
When `rollForwardOnNoCandidateFx` is disabled (set to `0` which is not the default) the existing behavior is to never roll forward to a pre-release version. If the settings is any other value (Minor/Major) it would roll forward to pre-release version if there's no available matching release version.

So for example, the machine has only `3.0.1-preview.1` installed and the application has reference to `3.0.0`. The existing behavior is
* Default behavior is to roll forward to the `3.0.1-preview.1` since there's no matching release version, and run the app.
* If the `rollForwardOnNoCandidateFx=0` (and only then), the app will fail to run (as it won't roll forward to pre-release version).

The new behavior will be to treat all settings of `rollForwardOnNoCandidateFx` the same with regard to pre-release. That is release version will roll forward to pre-release if there's no release version available. In the above sample, the app would run using the `3.0.1-preview.1` framework.

### Test names
All the tests which change behavior due to intended changes - should we rename them where applicable?

# TODOs

* No implementation for pre-release handling yet
* No implementation of `DOTNET_ROLL_FORWARD_TO_PRERELEASE`
  * `fx_reference::is_roll_forward_compatible` - checks that release can't roll forward to pre-release.
* Update SDK to show correct command line usage for `--roll-forward`
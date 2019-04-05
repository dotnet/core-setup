# Breaking changes

### Roll on patches only never rolls from release to pre-release
When `rollForwardOnNoCandidateFx` is disabled (set to `0` which is not the default) the existing behavior is to never roll forward to a pre-release version. If the settings is any other value (Minor/Major) it would roll forward to pre-release version if there's no available matching release version.

So for example, the machine has only `3.0.1-preview.1` installed and the application has reference to `3.0.0`. The existing behavior is
* Default behavior is to roll forward to the `3.0.1-preview.1` since there's no matching release version, and run the app.
* If the `rollForwardOnNoCandidateFx=0` (and only then), the app will fail to run (as it won't roll forward to pre-release version).

The new behavior will be to treat all settings of `rollForwardOnNoCandidateFx` the same with regard to pre-release. That is release version will roll forward to pre-release if there's no release version available. In the above sample, the app would run using the `3.0.1-preview.1` framework.

### Test names
All the tests which change behavior due to intended changes - should we rename them where applicable?

### Potential ordering problem
In case of many nested frameworks it's potentially possible to run into ordering issues.
In this sample NETCore is available as 2.1.1 and 2.2.0
```
HigherLevelFX
 -> NETCore 2.1.0 Minor
 -> MiddleWareFX 1.0.0
MiddleWareFX
 -> NETCore 2.1.0 LatestMinor
```
This would resolve to `NETCore 2.1.1` - because the reference from `HigherLevelFX` is hard resolved first, and later on when the reference from `MiddleWareFX` is processed, it's compatible.

On the other hand
```
HigherLevelFX
 -> MiddleWareFX 1.0.0
 -> NETCore 2.1.0 Minor
MiddleWareFX
 -> NETCore 2.1.0 LatestMinor
```
(Same except the order of references from `HigherLevelFX` is swapped)
This would resolve to `NETCore 2.2.0` - because the reference from `MiddleWareFX` is hard resolved first, and later on the reference from `HigherLevelFX` is compatible.

Document this, and add tests for this as well.
Note that this is existing behavior, instead of Minor/LatestMinor use LatestPatch and LatestPatch/applyPatches=false which both can be specified in 2.*. In one case it would roll forward to latest patch, in the other it would not.

# TODOs

* No implementation of `DOTNET_ROLL_FORWARD_TO_PRERELEASE`
  * `fx_reference::is_roll_forward_compatible` - checks that release can't roll forward to pre-release.
* Update SDK to show correct command line usage for `--roll-forward`
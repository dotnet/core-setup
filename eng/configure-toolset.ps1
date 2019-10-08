# Always use the local repo packages directory instead of the user's NuGet cache
# to keep the same between "ci" and non-"ci" builds. If the efficiency gain is
# required and it's worth maintaining the different types of build, this can be
# removed.
$script:useGlobalNuGetCache = $false

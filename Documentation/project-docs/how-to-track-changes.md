# How to track changes

Recently I checked in a change to one of our various dotnet repos (coreclr) and I wanted to figure out when the change would appear in our various NuGet packages
and installers. I didn't know how the changes flowed through our build, setup, and publication processes so I asked around (thanks Davis!) and wrote up 
what I learned. If you discover this doc is inaccurate or incomplete, kindly update it! 


## The starting point

I assume you have already identified a git commit in some repo and you know the git commit hash. In my case it was: [435a69a9aca037f348a09e00dffef8a0888682a7 in the coreclr repo](https://github.com/dotnet/coreclr/commit/435a69a9aca037f348a09e00dffef8a0888682a7)


## Step 1: A nightly build for your repo gets pushed to MyGet

For many of the dotnet repos such as CoreCLR the daily builds are posted to the [dotnet-core](https://dotnet.myget.org/gallery/dotnet-core) feed. Other repos or specific branches might
publish elsewhere in which case hopefully you can find that in the repo docs or by searching [the gallery](https://dotnet.myget.org/gallery) for a appropriate sounding feed name.

If you know what NuGet package has the binaries you need you can search for it in the feed, but if not you can narrow the search by first checking the [dotnet/versions](https://github.com/dotnet/versions)
repo. Whenever a dotnet repo publishes new builds it makes a commit in the versions repo indicating which packages got published. For example during the night after I merged my change [this commit](https://github.com/dotnet/versions/commit/cf8930fbe52e5eacf8ab0d7fb06f032d19cda5d5#diff-5f6099c37f777c410c4397b3f1e38870)
showed up. Looking at the [Last_Build_Packages.txt](https://github.com/dotnet/versions/blob/master/build-info/dotnet/coreclr/master/Last_Build_Packages.txt) that commit edited you can see a list of
NuGet packages that CoreCLR repo publishes. My change went into coreclr.dll which is a native binary so picking a package with CoreCLR and a particular OS + architecture in the name is probably going
to be the package I want.

(For CoreCLR at least there are both some packages that start with "transport.*" and some that don't. I assume this is an implementation detail of the build system and not one I spent time to decipher)

From the MyGet feed I navigated to the [package I wanted](https://dotnet.myget.org/feed/dotnet-core/package/nuget/runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR). Right in the description text
there is an embedded git commit hash and lower on the page is the Package History section with links to every build of the package that has been published. For example the [2.2.0-preview1-26608-04](https://dotnet.myget.org/feed/dotnet-core/package/nuget/runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR/2.2.0-preview1-26608-04) build
was produced about the right time and the description says:

    Internal implementation package not meant for direct consumption. Please do not reference directly. The .NET Core runtime,
    called CoreCLR, and the base library, called System.Private.CoreLib. It includes the garbage collector, JIT compiler, base .NET
    data types and many low-level classes. 311322beb96c5475fd7030fcd2f6e7ff14918853 When using NuGet 3.x this package
    requires at least version 3.4.

Then I confirmed that commit [311322beb96c5475fd7030fcd2f6e7ff14918853](https://github.com/dotnet/coreclr/commit/311322beb96c5475fd7030fcd2f6e7ff14918853) was more recent than my commit (it is). Occasionally
you might find that even though a build was produced at a later wall clock time than your commit it doesn't have your changes in it. Most likely search the next build after that and your changes will be present
but worst case you can binary search all the builds. In case your NuGet package doesn't kindly include the commit hash in the description you may also be able to find it by downloading the NuGet package and
searching for a version.txt file in the root of the package (.nupkg files can be renamed .zip and then opened with any tool that supports .zip). A final place to search for the hash is in the file version information
embedded in the binaries.

Now I know my change is present in CoreCLR builds/MyGet packages >= 2.2.0-preview1-26608-04

## Step 2: Find builds of Microsoft.NetCore.App or installers which aggregate the changes from your repo

### Microsoft.NetCore.App NuGet package

In the [commit history of the core-setup repo](https://github.com/dotnet/core-setup/commits/master) you are looking for commits that say 'Update your_repo_name to some_version' where some_version is at least
as recent as the one that has your commit. [This one](https://github.com/dotnet/core-setup/commit/8a48d863ad01ccd0763b7f3fab487503f5b75625) updated CoreCLR to preview1-26609-02. Then you search the MyGet feed
for the [Microsoft.NetCore.App package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App) looking for one with an embedded git hash in the description text that is at least as recent. 
In this case [this package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App/2.2.0-preview1-26610-01) was exactly at that commit.

If you have a particular Microsoft.NETCore.App NuGet package already downloaded you can look at Microsoft.NETCore.App.versions.txt in the root of the package to see the git hashes of various repos that were used to compose the build.

### Daily build installers

Daily builds for both the runtime and the full SDK can be found [here](https://github.com/dotnet/core/blob/master/daily-builds.md). If you download one of these in zip form you can browse to .\shared\Microsoft.NETCore.App
and then build number of the M.N.A package is used as the subdirectory name.
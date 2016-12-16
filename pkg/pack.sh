#!/usr/bin/env bash

init_distro_name()
{
    if [ ! -e /etc/os-release ]; then
        echo "WARNING: Can not determine runtime id for current distro."
        export __distro_rid=""
    else
        source /etc/os-release
        export __distro_rid="$ID.$VERSION_ID-x64"
    fi
}

usage()
{
    echo "Usage: $0 --rid <Runtime Identifier>"
    echo ""
    echo "Options:"
    echo "  --rid <Runtime Identifier>        Target Runtime Identifier"

    exit 1
}

set -e

# determine current directory
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done

# initialize variables
__project_dir="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
__distro_rid=

while [ "$1" != "" ]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -h|--help)
            usage
            exit 1
            ;;
        --rid)
            shift
            __distro_rid=$1
            ;;
        *)
        echo "Unknown argument to pack.sh $1"; exit 1
    esac
    shift
done

# setup msbuild
"$__project_dir/init-tools.sh"

# acquire dependencies
pushd "$__project_dir/deps"
"$__project_dir/Tools/dotnetcli/dotnet" restore --configfile "$__project_dir/../NuGet.Config" --disable-parallel --packages "$__project_dir/packages"
popd

# cleanup existing packages
rm -rf $__project_dir/bin

# build to produce nupkgs
__msbuild="$__project_dir/Tools/msbuild.sh"

__targets_param=
if [ "$(uname -s)" == "Darwin" ]; then
    __targets_param="TargetsOSX=true"
else
    __targets_param="TargetsLinux=true"
    if [ -z $__distro_rid ]; then
        init_distro_name
    fi
fi

__common_parameters="/p:$__targets_param /p:DistroRid=$__distro_rid /verbosity:minimal"

$__msbuild $__project_dir/projects/packages.builds $__common_parameters || exit 1

exit 0

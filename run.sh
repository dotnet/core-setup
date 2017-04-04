#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

# Use in the the functions: eval $invocation
invocation='echo "Calling: ${FUNCNAME[0]}"'

# args:
# remote_path - $1
# [out_path] - $2 - stdout if not provided
download() {
    eval $invocation
    
    local remote_path=$1
    local out_path=${2:-}

    local failed=false
    if [ -z "$out_path" ]; then
        curl --retry 10 -sSL --create-dirs $remote_path || failed=true
    else
        curl --retry 10 -sSL --create-dirs -o $out_path $remote_path || failed=true
    fi
    
    if [ "$failed" = true ]; then
        echo "run-build: Error: Download failed" >&2
        return 1
    fi
}

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__toolsLocalPath=$__scriptpath/Tools
__bootstrapScript=$__toolsLocalPath/bootstrap.sh
__packagesPath=$__scriptpath/Packages

if [ ! -f $__bootstrapScript ]; then
    if [ ! -d $__toolsLocalPath ]; then
        mkdir $__toolsLocalPath
    fi
    download "https://raw.githubusercontent.com/dotnet/buildtools/master/bootstrap/bootstrap.sh" "$__bootstrapScript"
    chmod u+x $__bootstrapScript
    # create packages folder with dummy global.json to avoid conflicts with root's global json
    if [ ! -f $__packagesPath ]; then
        mkdir $__packagesPath
        echo '{ "projects": [] }' > $__packagesPath/global.json
    fi
fi

# Increases the file descriptors limit for this bash. It prevents an issue we were hitting during restore
FILE_DESCRIPTOR_LIMIT=$( ulimit -n )
if [ $FILE_DESCRIPTOR_LIMIT -lt 1024 ]
then
    ulimit -n 1024
fi

$__bootstrapScript --toolsLocalPath $__toolsLocalPath --dotNetInstallBranch "rel/1.0.0-preview2.1" > bootstrap.log

if [ $? != 0 ]; then
    echo "run-build: Error: Boot-strapping failed with exit code $?, see bootstrap.log for more information." >&2
    exit $?
fi


__dotnet=$__toolsLocalPath/dotnetcli/dotnet

$__dotnet $__toolsLocalPath/run.exe $*
exit $?
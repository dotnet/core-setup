#!/bin/sh
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
echo "Creating dotnet host symbolic link: /usr/bin/dotnet"
ln -sf "/usr/share/dotnet/dotnet" "/usr/bin/dotnet"
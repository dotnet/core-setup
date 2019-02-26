#!/bin/sh
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
# Run the script only when newer version is not installed. Skip during package upgrade 
if [ $1 = 0 ]; then
   echo "Removing dotnet host symbolic link"
   unlink /usr/bin/dotnet
fi
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 3.14)
project(nativehost_singlefile)

set(DOTNET_PROJECT_NAME "nativehost_singlefile")

add_definitions(-DEXPORT_SHARED_API=1)
include(../nativehost.cmake)

set_target_properties(${DOTNET_PROJECT_NAME} PROPERTIES ENABLE_EXPORTS ON)
target_sources(${DOTNET_PROJECT_NAME} PRIVATE ./singlefile.cpp)
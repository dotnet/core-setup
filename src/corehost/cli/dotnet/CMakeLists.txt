# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(dotnet)
set(DOTNET_PROJECT_NAME "dotnet")
set(SOURCES 
    ../fxr/fx_ver.cpp)

if(WIN32)
    list(APPEND SOURCES
        dotnet.manifest)
endif()

include(../exe.cmake)

install_library_and_symbols (dotnet)

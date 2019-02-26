# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(hostfxr)

set(DOTNET_PROJECT_NAME "hostfxr")

# Include directories
include_directories(../json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../../common/trace.cpp
    ../../common/utils.cpp
    ../libhost.cpp
    ../deps_format.cpp
    ../deps_entry.cpp
    ../host_startup_info.cpp
    ../runtime_config.cpp
    ../json/casablanca/src/json/json.cpp
    ../json/casablanca/src/json/json_parsing.cpp
    ../json/casablanca/src/json/json_serialization.cpp
    ../json/casablanca/src/utilities/asyncrt_utils.cpp
    ../fx_definition.cpp
    ../fx_reference.cpp
    ../version.cpp
    ./hostfxr.cpp
    ./fx_ver.cpp
    ./fx_muxer.cpp
    ./fx_muxer.messages.cpp
    ./framework_info.cpp
    ./sdk_info.cpp
    ./sdk_resolver.cpp
)

include(../lib.cmake)

install_library_and_symbols (hostfxr)

# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(nethost)

set(DOTNET_PROJECT_NAME "nethost")

# Include directories
include_directories(../fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    nethost.cpp
    ../fxr_resolver.cpp
    ../fxr/fx_ver.cpp
)

include(../lib.cmake)

add_definitions(-DFEATURE_LIBHOST=1)
add_definitions(-DNETHOST_EXPORT)

if("${CLI_CMAKE_NETHOST_VER}" STREQUAL "")
    message(FATAL_ERROR "nethost version is not specified")
else()
    add_definitions(-DLIBHOST_PKG_VER="${CLI_CMAKE_NETHOST_VER}")
endif()

install(TARGETS nethost DESTINATION corehost)
install_symbols(nethost corehost)
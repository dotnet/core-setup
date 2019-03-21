# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(nativehost)

set(DOTNET_PROJECT_NAME "nativehost")

include_directories(${CMAKE_CURRENT_LIST_DIR}/../../nethost)

set(SOURCES
    ./nativehost.cpp
)

include(../testexe.cmake)

set_common_libs("exe")
target_link_libraries(${DOTNET_PROJECT_NAME} nethost)
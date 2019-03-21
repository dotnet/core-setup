# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.8.12)
project(nativehost)

set(DOTNET_PROJECT_NAME "nativehost")

set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
set(MACOSX_RPATH ON)
if (CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(CMAKE_INSTALL_RPATH "@loader_path")
else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
endif()

include_directories(${CMAKE_CURRENT_LIST_DIR}/../../nethost)

set(SOURCES
    ./nativehost.cpp
)

include(../testexe.cmake)

set_common_libs("exe")
target_link_libraries(${DOTNET_PROJECT_NAME} nethost)
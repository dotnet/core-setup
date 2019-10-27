# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.8.12)
project(nativehost_common)

set(DOTNET_PROJECT_NAME "nativehost_common")

include(${CMAKE_CURRENT_LIST_DIR}/../../../common.cmake)

set(SOURCES
    ../error_writer_redirector.cpp
    ../hostfxr_exports.cpp
    ../hostpolicy_exports.cpp
    ../host_context_test.cpp
    ../resolve_component_dependencies_test.cpp
)

set(HEADERS
    ../error_writer_redirector.h
    ../hostfxr_exports.h
    ../hostpolicy_exports.h
    ../host_context_test.h
    ../resolve_component_dependencies_test.h
)

if(WIN32)
    list(APPEND SOURCES
        ../comhost_test.cpp)

    list(APPEND HEADERS
        ../comhost_test.h)
endif()

add_library(${DOTNET_PROJECT_NAME} STATIC ${SOURCES})
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(comhost)

include(../setup.cmake)

# Include directories
include_directories(./)
include_directories(../)
include_directories(../fxr)
include_directories(../../)
include_directories(../../common)

set(SOURCES
    ijwthunk.cpp
    ijwhost.cpp
    exports.cpp
    ../fxr_resolver.cpp
    ../../common/trace.cpp
    ../../common/utils.cpp
    ../fxr/fx_ver.cpp
    pedecoder.cpp
)

if(WIN32)
    list(APPEND SOURCES
        ../../common/pal.windows.cpp
        ../../common/longfile.windows.cpp)
else()
    list(APPEND SOURCES
        ../../common/pal.unix.cpp
        ${VERSION_FILE_PATH})
endif()

# Move to setup.cmake if IJWHOST is ever built on non-Windows
if("${CLI_CMAKE_IJWHOST_VER}" STREQUAL "")
    message(FATAL_ERROR "ijwhost version is not specified")
else()
    add_definitions(-DLIBHOST_PKG_VER="${CLI_CMAKE_IJWHOST_VER}")
endif()

if(CLI_CMAKE_PLATFORM_ARCH_I386)
    set(THUNK_STUB_SOURCES
        i386/asmhelpers.asm
        i386/IJWBootstrapThunkCPU.cpp
    )
elseif(CLI_CMAKE_PLATFORM_ARCH_AMD64)
    set(THUNK_STUB_SOURCES
        AMD64/asmhelpers.asm
        AMD64/IJWBootstrapThunkCPU.cpp
    )
elseif(CLI_CMAKE_PLATFORM_ARCH_ARM)
    set(THUNK_STUB_SOURCES
        arm/asmhelpers.asm
        arm/IJWBootstrapThunkCPU.cpp
    )
elseif(CLI_CMAKE_PLATFORM_ARCH_ARM64)
    set(THUNK_STUB_SOURCES
        arm64/asmhelpers.asm
        arm64/IJWBootstrapThunkCPU.cpp
    )
endif()

add_definitions(-DEXPORT_SHARED_API=1)
add_definitions(-DFEATURE_LIBHOST=1)

add_library(ijwhost SHARED ${SOURCES} ${THUNK_STUB_SOURCES})

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))
    target_link_libraries(ijwhost Advapi32.lib Ole32.lib Shell32.lib)
endif()

install_library_and_symbols (ijwhost) 
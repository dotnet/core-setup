# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(comhost)

include(../setup.cmake)

# Include directories
include_directories(./)
include_directories(../)
include_directories(../../)
include_directories(../../common)
include_directories(../json/casablanca/include)

set(SOURCES
    exports.cpp
    clsidmap.cpp
    ../../common/trace.cpp
    ../../common/utils.cpp
    ../json/casablanca/src/json/json.cpp
    ../json/casablanca/src/json/json_parsing.cpp
    ../json/casablanca/src/json/json_serialization.cpp
    ../json/casablanca/src/utilities/asyncrt_utils.cpp
)

if(WIN32)
    list(APPEND SOURCES
        ../../common/pal.windows.cpp
        ../../common/longfile.windows.cpp
        Exports.def)
else()
    list(APPEND SOURCES
        ../../common/pal.unix.cpp
        ${VERSION_FILE_PATH})
endif()

add_definitions(-DCOREHOST_MAKE_DLL=1)

add_library(comhost SHARED ${SOURCES})
install_library_and_symbols (comhost)
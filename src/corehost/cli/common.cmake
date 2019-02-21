# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project(${DOTNET_PROJECT_NAME})

if(WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
else()
    add_compile_options(-fPIC)
    add_compile_options(-fvisibility=hidden)
endif()

include(../setup.cmake)

# Include directories
if(WIN32)
    include_directories("${CLI_CMAKE_RESOURCE_DIR}/${DOTNET_PROJECT_NAME}")
endif()
include_directories(./)
include_directories(../)
include_directories(../../)
include_directories(../../common)

if(WIN32)
    list(APPEND SOURCES 
        ../../common/pal.windows.cpp
        ../../common/longfile.windows.cpp)
else()
    list(APPEND SOURCES
        ../../common/pal.unix.cpp
        ${VERSION_FILE_PATH})
endif()

set(RESOURCES)
if(WIN32 AND NOT SKIP_VERSIONING)
    list(APPEND RESOURCES ../native.rc)
endif()

# Specify the import library to link against for Arm32 build since the default set is minimal
if (WIN32 AND CLI_CMAKE_PLATFORM_ARCH_ARM)
    target_link_libraries(${DOTNET_PROJECT_NAME} shell32.lib)
endif()

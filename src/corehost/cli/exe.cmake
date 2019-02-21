# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
project (${DOTNET_PROJECT_NAME})

include(../common.cmake)

# Include directories
include_directories(../fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ../../corehost.cpp
    ../../common/trace.cpp
    ../../common/utils.cpp)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES} ${RESOURCES})

if(NOT WIN32)
    disable_pax_mprotect(${DOTNET_PROJECT_NAME})
endif()

install(TARGETS ${DOTNET_PROJECT_NAME} DESTINATION bin)

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux|FreeBSD")
    target_link_libraries (${DOTNET_PROJECT_NAME} "pthread")
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    target_link_libraries (${DOTNET_PROJECT_NAME} "dl")
endif()




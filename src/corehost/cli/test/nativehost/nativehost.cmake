# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
set(MACOSX_RPATH ON)
if (CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(CMAKE_INSTALL_RPATH "@loader_path")
else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
endif()

include_directories(${CMAKE_CURRENT_LIST_DIR}/../../nethost)

if(WIN32)
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DELAYLOAD:nethost.dll")
endif()

set(SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/nativehost.cpp
)

include(${CMAKE_CURRENT_LIST_DIR}/../testexe.cmake)

target_link_libraries(${DOTNET_PROJECT_NAME} nethost)
target_link_libraries(${DOTNET_PROJECT_NAME} nativehost_common)

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))
    target_link_libraries(${DOTNET_PROJECT_NAME} Advapi32.lib Ole32.lib OleAut32.lib)
endif()
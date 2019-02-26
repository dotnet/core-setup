# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(${DOTNET_PROJECT_NAME})

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
add_definitions(-DEXPORT_SHARED_API=1)

add_library(${DOTNET_PROJECT_NAME} SHARED ${SOURCES} ${RESOURCES})

set_target_properties(${DOTNET_PROJECT_NAME} PROPERTIES MACOSX_RPATH TRUE)

# Specify the import library to link against for Arm32 build since the default set is minimal
if (WIN32 AND CLI_CMAKE_PLATFORM_ARCH_ARM)
    target_link_libraries(${DOTNET_PROJECT_NAME} shell32.lib)
endif()
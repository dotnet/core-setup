# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(${DOTNET_PROJECT_NAME}s)

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
add_definitions(-DEXPORT_SHARED_API=1)

add_library(${DOTNET_PROJECT_NAME}s STATIC ${SOURCES} ${RESOURCES})

set_target_properties(${DOTNET_PROJECT_NAME}s PROPERTIES MACOSX_RPATH TRUE)

set_common_libs("lib")

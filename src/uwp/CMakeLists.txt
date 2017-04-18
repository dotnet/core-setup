# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

include(../settings.cmake)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
include_directories("${CLI_CMAKE_RESOURCE_DIR}/dotnet")
add_subdirectory(Host)
#if (CLI_CMAKE_PLATFORM_ARCH_I386)
  add_subdirectory(CopyWin32Resources)
#endif ()
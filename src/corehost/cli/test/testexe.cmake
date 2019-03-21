# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project(${DOTNET_PROJECT_NAME})

set(SKIP_VERSIONING 1)

include(${CMAKE_CURRENT_LIST_DIR}/../common.cmake)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES})

install(TARGETS ${DOTNET_PROJECT_NAME} DESTINATION corehost_test)
install_symbols(${DOTNET_PROJECT_NAME} corehost_test)
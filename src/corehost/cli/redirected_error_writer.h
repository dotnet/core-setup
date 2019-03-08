// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef _COREHOST_CLI_REDIRECTED_ERROR_WRITER_H_
#define _COREHOST_CLI_REDIRECTED_ERROR_WRITER_H_

#include <pal.h>

void reset_redirected_error_writer();

void redirected_error_writer(const pal::char_t* msg);

pal::string_t get_redirected_error_string();

#endif /* _COREHOST_CLI_REDIRECTED_ERROR_WRITER_H_ */
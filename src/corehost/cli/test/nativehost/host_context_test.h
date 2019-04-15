// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include <pal.h>
#include <error_codes.h>
#include <hostfxr.h>

namespace host_context_test
{
    enum check_properties
    {
        none,
        get,
        set,
        remove,
        get_all
    };

    check_properties check_properties_from_string(const pal::char_t *str);

    bool app(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *app_path,
        int argc,
        const pal::char_t *argv[]);
    bool config(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[]);
    bool config_multiple(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *config_path,
        const pal::char_t *secondary_config_path,
        int argc,
        const pal::char_t *argv[]);
    bool mixed(
        check_properties scenario,
        const pal::string_t &hostfxr_path,
        const pal::char_t *app_path,
        const pal::char_t *config_path,
        int argc,
        const pal::char_t *argv[]);
}
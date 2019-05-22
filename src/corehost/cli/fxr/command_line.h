// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __COMMAND_LINE_H__
#define __COMMAND_LINE_H__

#include <host_interface.h>
#include <host_startup_info.h>
#include <pal.h>

typedef std::unordered_map<pal::string_t, std::vector<pal::string_t>> opt_map_t;

enum class known_options
{
    additional_probing_path,
    deps_file,
    runtime_config,
    fx_version,
    roll_forward,
    additional_deps,
    roll_forward_on_no_candidate_fx,

    __last // Sentinel value
};

namespace command_line
{
    pal::string_t get_last_known_arg(
        const opt_map_t& opts,
        known_options opt,
        const pal::string_t& de_fault);
    const pal::string_t& get_option_flag(known_options opt);

    // Returns '0' on success, 'AppArgNotRunnable' if should be routed to CLI, otherwise error code.
    int parse_args_for_mode(
        host_mode_t mode,
        const host_startup_info_t& host_info,
        const int argc,
        const pal::char_t* argv[],
        /*out*/ int *new_argoff,
        /*out*/ pal::string_t &app_candidate,
        /*out*/ opt_map_t &opts);
    int parse_args_for_sdk_command(
        const host_startup_info_t& host_info,
        const int argc,
        const pal::char_t* argv[],
        /*out*/ int *new_argoff,
        /*out*/ pal::string_t &app_candidate,
        /*out*/ opt_map_t &opts);

    void print_muxer_info(const pal::string_t &dotnet_root);
    void print_muxer_usage(bool is_sdk_present);
};

#endif // __COMMAND_LINE_H__
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __RUNTIME_CONFIG_H__
#define __RUNTIME_CONFIG_H__

#include <list>

#include "pal.h"
#include "cpprest/json.h"

typedef web::json::value json_value;

enum roll_fwd_on_no_candidate_fx_option
{
    roll_fwd_disabled = 0,
    roll_fwd_minor,
    roll_fwd_major_or_minor
};

class runtime_config_t
{
public:
    runtime_config_t();
    void parse(const pal::string_t& path, const pal::string_t& dev_path, const runtime_config_t* defaults);
    bool is_valid() const { return m_valid; }
    const pal::string_t& get_path() const { return m_path; }
    const pal::string_t& get_dev_path() const { return m_dev_path; }
    const pal::string_t& get_fx_version() const;
    void set_fx_version(const pal::string_t& value);
    const pal::string_t& get_fx_name() const;
    const pal::string_t& get_tfm() const;
    const std::list<pal::string_t>& get_probe_paths() const;
    bool get_patch_roll_fwd() const;
    bool get_prerelease_roll_fwd() const;
    enum roll_fwd_on_no_candidate_fx_option get_roll_fwd_on_no_candidate_fx() const;
    void set_roll_fwd_on_no_candidate_fx(enum roll_fwd_on_no_candidate_fx_option value);
    bool get_portable() const;
    bool parse_opts(const json_value& opts);
    void combine_properties(std::unordered_map<pal::string_t, pal::string_t>& combined_properties) const;

private:
    bool ensure_parsed();
    bool ensure_dev_config_parsed();

    std::unordered_map<pal::string_t, pal::string_t> m_properties;
    std::vector<std::string> m_prop_keys;
    std::vector<std::string> m_prop_values;
    std::list<pal::string_t> m_probe_paths;
    pal::string_t m_tfm;
    pal::string_t m_fx_name;
    pal::string_t m_fx_ver;
    bool m_patch_roll_fwd;
    bool m_prerelease_roll_fwd;
    enum roll_fwd_on_no_candidate_fx_option m_roll_fwd_on_no_candidate_fx;

    pal::string_t m_dev_path;
    pal::string_t m_path;
    bool m_portable;
    bool m_valid;
};
#endif // __RUNTIME_CONFIG_H__
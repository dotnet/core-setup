// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FX_REFERENCE_H__
#define __FX_REFERENCE_H__

#include <list>
#include "pal.h"
#include "fx_ver.h"
#include "roll_forward_option.h"

class fx_reference_t
{
public:
    fx_reference_t()
        : has_apply_patches(false)
        , apply_patches(false)
        , has_roll_forward(false)
        , roll_forward(roll_forward_option::Disable)
        , use_exact_version(false)
        , fx_name(_X(""))
        , fx_version(_X(""))
        , fx_version_number()
        { }

    const pal::string_t& get_fx_name() const
    {
        return fx_name;
    }
    void set_fx_name(const pal::string_t& value)
    {
        fx_name = value;
    }

    const pal::string_t& get_fx_version() const
    {
        return fx_version;
    }
    void set_fx_version(const pal::string_t& value)
    {
        fx_version = value;

        fx_ver_t::parse(fx_version, &fx_version_number);
    }

    const fx_ver_t& get_fx_version_number() const
    {
        return fx_version_number;
    }

    const bool* get_apply_patches() const
    {
        return (has_apply_patches ? &apply_patches : nullptr);
    }
    void set_apply_patches(bool value)
    {
        has_apply_patches = true;
        apply_patches = value;
    }

    const bool get_use_exact_version() const
    {
        return use_exact_version;
    }
    void set_use_exact_version(bool value)
    {
        use_exact_version = value;
    }

    const roll_forward_option* get_roll_forward() const
    {
        return (has_roll_forward ? &roll_forward : nullptr);
    }
    void set_roll_forward(roll_forward_option value)
    {
        has_roll_forward = true;
        roll_forward = value;
    }

    // Is the current version compatible with another instance with roll-forward semantics.
    bool is_roll_forward_compatible(const fx_ver_t& other) const;

    // Copy over any non-null values
    void apply_settings_from(const fx_reference_t& from);

    // Apply the most restrictive settings
    // Returns true if any settings were modified, false if nothing was updated (this has more restrictive settings then from)
    bool merge_roll_forward_settings_from(const fx_reference_t& from);

private:
    bool has_apply_patches;
    bool apply_patches;

    bool has_roll_forward;
    roll_forward_option roll_forward;

    bool use_exact_version;

    pal::string_t fx_name;

    pal::string_t fx_version;
    fx_ver_t fx_version_number;
};

typedef std::vector<fx_reference_t> fx_reference_vector_t;
typedef std::unordered_map<pal::string_t, fx_reference_t> fx_name_to_fx_reference_map_t;

#endif // __FX_REFERENCE_H__

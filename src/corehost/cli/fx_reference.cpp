// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal.h"
#include "fx_ver.h"
#include "fx_reference.h"
#include "roll_fwd_on_no_candidate_fx_option.h"

bool fx_reference_t::is_roll_forward_compatible(const fx_ver_t& other) const
{
    // We expect the version to be <
    assert(get_fx_version_number() < other);

    if (get_fx_version_number() == other)
    {
        return true;
    }

    if (get_use_exact_version())
    {
        return false;
    }

    // Verify major roll forward
    static_assert(
        roll_forward_option::LatestMajor > roll_forward_option::Major,
        "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    if (get_fx_version_number().get_major() != other.get_major()
        && roll_forward < roll_forward_option::Major)
    {
        return false;
    }

    // Verify minor roll forward
    static_assert(
        roll_forward_option::LatestMinor > roll_forward_option::Minor,
        "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    static_assert(
        roll_forward_option::Major > roll_forward_option::LatestMinor,
        "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    if (get_fx_version_number().get_minor() != other.get_minor()
        && roll_forward < roll_forward_option::Minor)
    {
        return false;
    }

    // Verify patch roll forward
    // We do not distinguish here whether a previous framework reference found a patch version based on:
    //  - initial reference matching a patch version,
    //  - or roll_forward=major\minor finding a compatible patch version as initial framework,
    //  - or applyPatches=true finding a newer patch version
    static_assert(
        roll_forward_option::Minor > roll_forward_option::LatestPatch,
        "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    static_assert(
        roll_forward_option::LatestPatch > roll_forward_option::Disabled,
        "Code assumes ordering of roll-forward options from least restrictive to most restrictive");
    if (get_fx_version_number().get_patch() != other.get_patch()
        && roll_forward == roll_forward_option::Disabled
        || (apply_patches == false && roll_forward == roll_forward_option::LatestPatch))
    {
        return false;
    }

    // Release cannot roll forward to pre-release
    if (!get_fx_version_number().is_prerelease() && other.is_prerelease())
    {
        return false;
    }

    return true;
}

void fx_reference_t::apply_settings_from(const fx_reference_t& from)
{
    if (from.get_fx_version().length() > 0)
    {
        set_fx_version(from.get_fx_version());
    }

    const roll_forward_option* from_roll_forward = from.get_roll_forward();
    if (from_roll_forward != nullptr)
    {
        set_roll_forward(*from_roll_forward);
    }

    const bool* from_apply_patches = from.get_apply_patches();
    if (from_apply_patches != nullptr)
    {
        set_apply_patches(*from_apply_patches);
    }
}

void fx_reference_t::merge_roll_forward_settings_from(const fx_reference_t& from)
{
    const roll_forward_option* from_roll_forward = from.get_roll_forward();
    if (from_roll_forward != nullptr)
    {
        const roll_forward_option* to_roll_forward = get_roll_forward();
        if (to_roll_forward == nullptr ||
            *from_roll_forward < *to_roll_forward)
        {
            set_roll_forward(*from_roll_forward);
        }
    }

    const bool* from_apply_patches = from.get_apply_patches();
    if (from_apply_patches != nullptr)
    {
        const bool* to_apply_patches = get_apply_patches();
        if (to_apply_patches == nullptr ||
            *from_apply_patches == false)
        {
            set_apply_patches(*from_apply_patches);
        }
    }
}

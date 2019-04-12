// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal.h"
#include "fx_ver.h"
#include "fx_reference.h"
#include "roll_fwd_on_no_candidate_fx_option.h"

bool fx_reference_t::is_compatible_with_higher_version(const fx_reference_t& higher_version_reference) const
{
    assert(get_fx_version_number() <= higher_version_reference.get_fx_version_number());

    if (get_fx_version_number() == higher_version_reference.get_fx_version_number())
    {
        return true;
    }

    if (get_use_exact_version())
    {
        return false;
    }

    // Verify major roll forward
    if (get_fx_version_number().get_major() != higher_version_reference.get_fx_version_number().get_major()
        && roll_forward < roll_forward_option::Major)
    {
        return false;
    }

    // Verify minor roll forward
    if (get_fx_version_number().get_minor() != higher_version_reference.get_fx_version_number().get_minor()
        && roll_forward < roll_forward_option::Minor)
    {
        return false;
    }

    // Verify patch roll forward
    if (get_fx_version_number().get_patch() != higher_version_reference.get_fx_version_number().get_patch()
        && roll_forward == roll_forward_option::LatestPatch
        && apply_patches == false)
    {
        return false;
    }

    // In here it means that either everything but pre-release part is the same, or the difference is OK
    // The roll-forward rules don't affect pre-release roll forward except when
    //  - rollForward is Disable - in which case no roll forward should occur, and the versions must exactly match
    //  - rollForward is LatestPatch and applyPatches=false - which would normally mean exactly the same as Disable, but
    //    for backward compat reasons this is a special case. In this case applyPatches is ignored for pre-release versions.
    //    So even if pre-release are different, the versions are compatible.
    if (roll_forward == roll_forward_option::Disable)
    {
        // We know the versions are different since we compared 100% equality above, so they're not compatible.
        // In here the versions could differ in patch or pre-release, in both cases they're not compatible.
        return false;
    }

    // Concernign pre-release versions
    //  - Pre-release is allowed to roll to any version (release or pre-release)
    //  - Release should prefer rolling to release, but is allowed to roll to pre-release if no compatible release is available
    // This function only compares framework references, it doesn't resolve framework reference to the available framework on disk.
    // As such it can't implement the "release should prefer release" as that requires the knowledge of all available versions.

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

bool fx_reference_t::merge_roll_forward_settings_from(const fx_reference_t& from)
{
    bool modified = false;
    const roll_forward_option* from_roll_forward = from.get_roll_forward();
    if (from_roll_forward != nullptr)
    {
        const roll_forward_option* to_roll_forward = get_roll_forward();
        if (to_roll_forward == nullptr ||
            *from_roll_forward < *to_roll_forward)
        {
            set_roll_forward(*from_roll_forward);
            modified = true;
        }
    }

    const bool* from_apply_patches = from.get_apply_patches();
    if (from_apply_patches != nullptr)
    {
        const bool* to_apply_patches = get_apply_patches();
        if (to_apply_patches == nullptr ||
            (*to_apply_patches == true && *from_apply_patches == false))
        {
            set_apply_patches(*from_apply_patches);
            modified = true;
        }
    }

    if (from.get_prefer_release() && !get_prefer_release())
    {
        set_prefer_release(true);
        modified = true;
    }

    return modified;
}

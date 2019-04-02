// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "fx_resolver.h"
#include "host_startup_info.h"
#include "trace.h"

namespace
{
    const int Max_Framework_Resolve_Retries = 100;

    fx_ver_t search_for_best_framework_match(
        const std::vector<fx_ver_t>& version_list,
        const pal::string_t& fx_ver,
        const fx_ver_t& specified,
        bool apply_patches,
        roll_forward_option roll_forward,
        bool release_only)
    {
        fx_ver_t best_match_version;

        static_assert(roll_forward_option::Disable < roll_forward_option::LatestPatch, "Assuming correct ordering of roll_forward_option values.");
        static_assert(roll_forward_option::Major > roll_forward_option::LatestMinor, "Assuming correct ordering of roll_forward_option values.");
        static_assert(roll_forward_option::LatestMajor > roll_forward_option::LatestMinor, "Assuming correct ordering of roll_forward_option values.");
        if (roll_forward > roll_forward_option::LatestPatch)
        {
            bool search_for_latest = roll_forward == roll_forward_option::LatestMinor || roll_forward == roll_forward_option::LatestMajor;

            trace::verbose(
                _X("'Roll forward' enabled with value [%d]. Looking for the %s %s greater than or equal version to [%s]"),
                roll_forward,
                search_for_latest ? _X("latest") : _X("least"),
                release_only ? _X("release") : _X("release/pre-release"),
                fx_ver.c_str());

            for (const auto& ver : version_list)
            {
                if ((!release_only || !ver.is_prerelease()) && ver >= specified)
                {
                    if (roll_forward <= roll_forward_option::LatestMinor)
                    {
                        // We only want to roll forward on minor
                        if (ver.get_major() != specified.get_major())
                        {
                            continue;
                        }

                        if (roll_forward <= roll_forward_option::LatestPatch)
                        {
                            if (ver.get_minor() != specified.get_minor())
                            {
                                continue;
                            }
                        }
                    }

                    best_match_version = (best_match_version == fx_ver_t())
                        ? ver
                        : (search_for_latest ? std::max(best_match_version, ver) : std::min(best_match_version, ver));
                }
            }

            if (best_match_version == fx_ver_t())
            {
                trace::verbose(_X("No match greater than or equal to [%s] found."), fx_ver.c_str());
            }
            else
            {
                trace::verbose(_X("Found version [%s]"), best_match_version.as_str().c_str());
            }
        }

        // For LatestMinor and LatestMajor the above search should already find the latest patch (it looks for latest version as a whole).
        // For Disable, there's no roll forward (in fact we should not even get here).
        // For LatestPatch, Major and Minor, we need to look for latest patch as the above would have find the lowest patch (as it looks for lowest version as a whole).
        //   For backward compatibility reasons we also need to consider the apply_patches setting though.
        if ((roll_forward == roll_forward_option::LatestPatch || roll_forward == roll_forward_option::Minor || roll_forward == roll_forward_option::Major)
            && apply_patches)
        {
            fx_ver_t apply_patch_from_version = best_match_version;
            if (apply_patch_from_version == fx_ver_t())
            {
                apply_patch_from_version = specified;
            }

            trace::verbose(
                _X("Applying patch roll forward from [%s] on %s"), 
                apply_patch_from_version.as_str().c_str(),
                release_only ? _X("release only") : _X("release/pre-release"));

            for (const auto& ver : version_list)
            {
                trace::verbose(_X("Inspecting version... [%s]"), ver.as_str().c_str());

                if ((!release_only || !ver.is_prerelease()) && ver >= apply_patch_from_version &&
                    ver.get_major() == apply_patch_from_version.get_major() &&
                    ver.get_minor() == apply_patch_from_version.get_minor())
                {
                    // Pick the greatest that differs only in patch.
                    best_match_version = std::max(ver, best_match_version);
                }
            }
        }

        return best_match_version;
    }

    fx_ver_t resolve_framework_version(
        const std::vector<fx_ver_t>& version_list,
        const pal::string_t& fx_ver,
        const fx_ver_t& specified,
        bool apply_patches,
        roll_forward_option roll_forward)
    {
        trace::verbose(_X("Attempting FX roll forward starting from [%s]"), fx_ver.c_str());

        // If the desired framework reference is release, then try release-only search first.
        if (!specified.is_prerelease())
        {
            fx_ver_t best_match_release_only = search_for_best_framework_match(
                version_list,
                fx_ver,
                specified,
                apply_patches,
                roll_forward,
                /*release_only*/ true);

            if (best_match_release_only != fx_ver_t())
            {
                return best_match_release_only;
            }
        }

        // If release-only didn't find anything, or the desired framework reference was pre-release
        // do a full search on all versions.
        fx_ver_t best_match = search_for_best_framework_match(
            version_list,
            fx_ver,
            specified,
            apply_patches,
            roll_forward,
            /*release_only*/ false);

        if (best_match == fx_ver_t())
        {
            // This is not strictly necessary, we just need to return version which doesn't exist.
            // But it's cleaner to return the desider reference then invalid -1.-1.-1 version.
            best_match = specified;
        }

        return best_match;
    }

    fx_definition_t* resolve_fx(
        const fx_reference_t & fx_ref,
        const pal::string_t & oldest_requested_version,
        const pal::string_t & dotnet_dir)
    {
        assert(!fx_ref.get_fx_name().empty());
        assert(!fx_ref.get_fx_version().empty());
        assert(fx_ref.get_apply_patches() != nullptr);
        assert(fx_ref.get_roll_forward() != nullptr);

        trace::verbose(_X("--- Resolving FX directory, name '%s' version '%s'"),
            fx_ref.get_fx_name().c_str(), fx_ref.get_fx_version().c_str());

        const auto fx_ver = fx_ref.get_fx_version();
        fx_ver_t specified;
        if (!fx_ver_t::parse(fx_ver, &specified, false))
        {
            trace::error(_X("The specified framework version '%s' could not be parsed"), fx_ver.c_str());
            return nullptr;
        }

        // Multi-level SharedFX lookup will look for the most appropriate version in several locations
        // by following the priority rank below:
        // .exe directory
        //  Global .NET directory
        // If it is not activated, then only .exe directory will be considered

        std::vector<pal::string_t> hive_dir;
        get_framework_and_sdk_locations(dotnet_dir, &hive_dir);

        pal::string_t selected_fx_dir;
        pal::string_t selected_fx_version;
        fx_ver_t selected_ver;

        for (pal::string_t dir : hive_dir)
        {
            auto fx_dir = dir;
            trace::verbose(_X("Searching FX directory in [%s]"), fx_dir.c_str());

            append_path(&fx_dir, _X("shared"));
            append_path(&fx_dir, fx_ref.get_fx_name().c_str());

            bool do_roll_forward = false;
            if (!fx_ref.get_use_exact_version())
            {
                // If production and no roll forward use given version.
                do_roll_forward = (*(fx_ref.get_apply_patches())) ||
                    ((*(fx_ref.get_roll_forward()) != roll_forward_option::LatestPatch) && (*(fx_ref.get_roll_forward()) != roll_forward_option::Disable));
            }

            if (!do_roll_forward)
            {
                trace::verbose(_X("Did not roll forward because apply_patches=%d, roll_forward=%d, use_exact_version=%d chose [%s]"),
                    *(fx_ref.get_apply_patches()), *(fx_ref.get_roll_forward()), fx_ref.get_use_exact_version(), fx_ver.c_str());

                append_path(&fx_dir, fx_ver.c_str());
                if (pal::directory_exists(fx_dir))
                {
                    selected_fx_dir = fx_dir;
                    selected_fx_version = fx_ver;
                    break;
                }
            }
            else
            {
                std::vector<pal::string_t> list;
                std::vector<fx_ver_t> version_list;
                pal::readdir_onlydirectories(fx_dir, &list);

                for (const auto& version : list)
                {
                    fx_ver_t ver;
                    if (fx_ver_t::parse(version, &ver, false))
                    {
                        version_list.push_back(ver);
                    }
                }

                fx_ver_t resolved_ver = resolve_framework_version(version_list, fx_ver, specified, *(fx_ref.get_apply_patches()), *(fx_ref.get_roll_forward()));

                pal::string_t resolved_ver_str = resolved_ver.as_str();
                append_path(&fx_dir, resolved_ver_str.c_str());

                if (pal::directory_exists(fx_dir))
                {
                    if (selected_ver != fx_ver_t())
                    {
                        // Compare the previous hive_dir selection with the current hive_dir to see which one is the better match
                        std::vector<fx_ver_t> version_list;
                        version_list.push_back(resolved_ver);
                        version_list.push_back(selected_ver);
                        resolved_ver = resolve_framework_version(version_list, fx_ver, specified, *(fx_ref.get_apply_patches()), *(fx_ref.get_roll_forward()));
                    }

                    if (resolved_ver != selected_ver)
                    {
                        trace::verbose(_X("Changing Selected FX version from [%s] to [%s]"), selected_fx_dir.c_str(), fx_dir.c_str());
                        selected_ver = resolved_ver;
                        selected_fx_dir = fx_dir;
                        selected_fx_version = resolved_ver_str;
                    }
                }
            }
        }

        if (selected_fx_dir.empty())
        {
            trace::error(_X("It was not possible to find any compatible framework version"));
            return nullptr;
        }

        trace::verbose(_X("Chose FX version [%s]"), selected_fx_dir.c_str());

        return new fx_definition_t(fx_ref.get_fx_name(), selected_fx_dir, oldest_requested_version, selected_fx_version);
    }
}

StatusCode fx_resolver_t::soft_roll_forward_helper(
    const fx_reference_t & higher_fx_ref,
    const fx_reference_t & lower_fx_ref,
    bool newest_is_hard_roll_forward)
{
    const pal::string_t& fx_name = higher_fx_ref.get_fx_name();
    fx_reference_t updated_newest = higher_fx_ref; // copy

    if (lower_fx_ref.get_fx_version_number() == higher_fx_ref.get_fx_version_number())
    {
        updated_newest.merge_roll_forward_settings_from(lower_fx_ref);
        m_newest_references[fx_name] = updated_newest;
        return StatusCode::Success;
    }

    if (lower_fx_ref.is_roll_forward_compatible(higher_fx_ref.get_fx_version_number()))
    {
        updated_newest.merge_roll_forward_settings_from(lower_fx_ref);
        m_newest_references[fx_name] = updated_newest;

        if (newest_is_hard_roll_forward)
        {
            display_retry_framework_trace(lower_fx_ref, higher_fx_ref);
            return StatusCode::FrameworkCompatRetry;
        }

        display_compatible_framework_trace(higher_fx_ref.get_fx_version(), lower_fx_ref);
        return StatusCode::Success;
    }

    // Error condition - not compatible with the other reference
    display_incompatible_framework_error(higher_fx_ref.get_fx_version(), lower_fx_ref);
    return StatusCode::FrameworkCompatFailure;
}

// Performs a "soft" roll-forward meaning we don't read any physical framework folders
// and just check if the lower_fx_ref reference is compatible with the higher_fx_ref reference
// with respect to roll-forward/apply-patches.
//  - fx_ref
//      The reference to resolve (the one we're processing).
//      Passed by-value to avoid side-effects with mutable newest_references and oldest_references.
//  - newest_is_hard_roll_forward
//      true if there is a reference to the framework specified by fx_ref in the newest_references
//      and that reference is pointing to a physically resolved framework on the disk (so the version in it
//      actually exists on disk).
//      This is used to restart the framework resolution if the soft-roll-forward results in updating
//      the m_newest_references for the processed framework with a higher version. In that case
//      it is necessary to throw away the results of the previous disk resolution and resolve the new
//      current reference against the frameworks available on disk.
//      If the current reference on the other hand has not been resolved against the disk yet
//      then we can safely move it forward to a higher version. It will be resolved against the disk eventually
//      but there's no work to throw away/retry yet.
StatusCode fx_resolver_t::soft_roll_forward(
    const fx_reference_t fx_ref,
    bool newest_is_hard_roll_forward)
{
    /*byval*/ fx_reference_t current_ref = m_newest_references[fx_ref.get_fx_name()];

    // Perform soft "in-memory" roll-forwards
    if (fx_ref.get_fx_version_number() >= current_ref.get_fx_version_number())
    {
        return soft_roll_forward_helper(fx_ref, current_ref, newest_is_hard_roll_forward);
    }

    assert(fx_ref.get_fx_version_number() < current_ref.get_fx_version_number());
    return soft_roll_forward_helper(current_ref, fx_ref, false);
}

// Processes one framework's runtime configuration.
// For the most part this is about resolving framework references.
// - host_info
//     Information about the host - mainly used to determine where to search for frameworks.
// - override_settings
//     Framework resolution settings which will win over anything found (settings comming from command line).
//     Passed as fx_reference_t for simplicity, the version part of that structure is ignored.
// - config
//     Parsed runtime configuration to process.
// - fx_definitions
//     List of "hard" resolved frameworks, that is frameworks actually found on the disk.
//     Frameworks are added to the list as they are resolved.
//     The order in the list is maintained such that the app is always the first and then the framework in their dependency order.
//     That means that the root framework (typically Microsoft.NETCore.App) is the last.
//     Frameworks are never removed as there's no operation which would "remove" a framework reference.
//     Frameworks are never updated in the list. If such operation is required, instead the function returns FrameworkCompatRetry
//     and the caller will restart the framework resolution process (with new fx_definitions).
// Return value
//     Success - all frameworks were successfully resolved and the final (disk resolved) frameworks are in the fx_definitions.
//     FrameworkCompatRetry - the resolution algorithm needs to restart as some of already processed references has changed.
//     FrameworkCompatFailure - the resolution failed with unrecoverable error which is due to framework resolution algorithm itself.
//     FrameworkMissingFailure - the resolution failed because the requested framework doesn't exist on disk.
//     InvalidConfigFile - reading of a runtime config for some of the processed frameworks has failed.
StatusCode fx_resolver_t::read_framework(
    const host_startup_info_t & host_info,
    const fx_reference_t & override_settings,
    const runtime_config_t & config,
    fx_definition_vector_t & fx_definitions)
{
    // Loop through each reference and update the list of newest references before we resolve_fx.
    // This reconciles duplicate references to minimize the number of resolve retries.
    for (const fx_reference_t& fx_ref : config.get_frameworks())
    {
        const pal::string_t& fx_name = fx_ref.get_fx_name();
        auto temp_ref = m_newest_references.find(fx_name);
        if (temp_ref == m_newest_references.end())
        {
            m_newest_references.insert({ fx_name, fx_ref });
            m_oldest_references.insert({ fx_name, fx_ref });
        }
        else
        {
            if (fx_ref.get_fx_version_number() < m_oldest_references[fx_name].get_fx_version_number())
            {
                m_oldest_references[fx_name] = fx_ref;
            }
        }
    }

    StatusCode rc = StatusCode::Success;

    // Loop through each reference and resolve the framework
    for (const fx_reference_t& fx_ref : config.get_frameworks())
    {
        const pal::string_t& fx_name = fx_ref.get_fx_name();

        auto existing_framework = std::find_if(
            fx_definitions.begin(),
            fx_definitions.end(),
            [&](const std::unique_ptr<fx_definition_t> & fx) { return fx_name == fx->get_name(); });

        if (existing_framework == fx_definitions.end())
        {
            // Perform a "soft" roll-forward meaning we don't read any physical framework folders yet
            // Since we didn't find the framework in the resolved list yet, it's a pure soft roll-forward
            // it's OK to update the newest reference as we haven't processed it yet.
            rc = soft_roll_forward(fx_ref, /*newest_is_hard_roll_forward*/ false);
            if (rc)
            {
                break; // Error case
            }

            fx_reference_t& newest_ref = m_newest_references[fx_name];

            // Resolve the framwork against the the existing physical framework folders
            fx_definition_t* fx = resolve_fx(newest_ref, m_oldest_references[fx_name].get_fx_version(), host_info.dotnet_root);
            if (fx == nullptr)
            {
                display_missing_framework_error(fx_name, newest_ref.get_fx_version(), pal::string_t(), host_info.dotnet_root);
                return FrameworkMissingFailure;
            }

            // Update the newest version based on the hard version found
            newest_ref.set_fx_version(fx->get_found_version());

            fx_definitions.push_back(std::unique_ptr<fx_definition_t>(fx));

            // Recursively process the base frameworks
            pal::string_t config_file;
            pal::string_t dev_config_file;
            get_runtime_config_paths(fx->get_dir(), fx_name, &config_file, &dev_config_file);
            fx->parse_runtime_config(config_file, dev_config_file, newest_ref, override_settings);

            runtime_config_t new_config = fx->get_runtime_config();
            if (!new_config.is_valid())
            {
                trace::error(_X("Invalid framework config.json [%s]"), new_config.get_path().c_str());
                return StatusCode::InvalidConfigFile;
            }

            rc = read_framework(host_info, override_settings, new_config, fx_definitions);
            if (rc)
            {
                break; // Error case
            }
        }
        else
        {
            // Perform a "soft" roll-forward meaning we don't read any physical framework folders yet
            // Note that since we found the framework in the already resolved frameworks
            // pass a flag which marks the newest resolved framework reference as "hard roll-forward"
            // meaning that if we need to update it, we need to restart the entire process.
            rc = soft_roll_forward(fx_ref, /*newest_is_hard_roll_forward*/ true);
            if (rc)
            {
                break; // Error or retry case
            }

            // Success but move it to the back (without calling dtors) so that lower-level frameworks come last including Microsoft.NetCore.App
            std::rotate(existing_framework, existing_framework + 1, fx_definitions.end());
        }
    }

    return rc;
}

fx_resolver_t::fx_resolver_t()
{
}

StatusCode fx_resolver_t::resolve_frameworks_for_app(
    const host_startup_info_t & host_info,
    const fx_reference_t & override_settings,
    const runtime_config_t & app_config,
    fx_definition_vector_t & fx_definitions)
{
    fx_resolver_t resolver;

    // Read the shared frameworks; retry is necessary when a framework is already resolved, but then a newer compatible version is processed.
    StatusCode rc = StatusCode::Success;
    int retry_count = 0;
    do
    {
        fx_definitions.resize(1); // Erase any existing frameworks for re-try
        rc = resolver.read_framework(host_info, override_settings, app_config, fx_definitions);
    } while (rc == StatusCode::FrameworkCompatRetry && retry_count++ < Max_Framework_Resolve_Retries);

    assert(retry_count < Max_Framework_Resolve_Retries);

    if (rc == StatusCode::Success)
    {
        display_summary_of_frameworks(fx_definitions, resolver.m_newest_references);
    }

    return rc;
}

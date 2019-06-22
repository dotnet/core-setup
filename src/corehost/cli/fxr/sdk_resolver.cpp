// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "sdk_resolver.h"

#include "cpprest/json.h"
#include "trace.h"
#include "utils.h"
#include "sdk_info.h"

typedef web::json::value json_value;
typedef web::json::object json_object;

using namespace std;

sdk_resolver::sdk_resolver(bool allow_prerelease) :
    sdk_resolver({}, sdk_roll_forward_policy::latest_major, allow_prerelease)
{
}

sdk_resolver::sdk_resolver(fx_ver_t requested, sdk_roll_forward_policy policy, bool allow_prerelease) :
    _requested(move(requested)),
    _policy(policy),
    _allow_prerelease(allow_prerelease)
{
}

pal::string_t const& sdk_resolver::global_file_path() const
{
    return _global_file_path;
}

fx_ver_t const& sdk_resolver::requested_version() const
{
    return _requested;
}

sdk_roll_forward_policy sdk_resolver::policy() const
{
    return _policy;
}

bool sdk_resolver::allow_prerelease() const
{
    return _allow_prerelease;
}

pal::string_t sdk_resolver::resolve(const pal::string_t& dotnet_root) const
{
    auto requested = _requested.is_empty() ? pal::string_t{} : _requested.as_str();

    trace::verbose(
        _X("Resolving SDKs with version = '%s', roll-forward = '%s', allow-prerelease = %s"),
        requested.empty() ? _X("latest") : requested.c_str(),
        to_policy_name(_policy),
        _allow_prerelease ? _X("true") : _X("false"));

    pal::string_t resolved_sdk_path;
    fx_ver_t resolved_version;

    vector<pal::string_t> locations;
    get_framework_and_sdk_locations(dotnet_root, &locations);

    for (auto&& dir : locations)
    {
        append_path(&dir, _X("sdk"));

        if (resolve_sdk_path_and_version(dir, resolved_sdk_path, resolved_version))
        {
            break;
        }
    }

    if (!resolved_sdk_path.empty())
    {
        trace::verbose(_X("SDK path resolved to [%s]"), resolved_sdk_path.c_str());
        return resolved_sdk_path;
    }

    if (!requested.empty())
    {
        if (!_global_file_path.empty())
        {
            trace::error(_X("A compatible installed .NET Core SDK for global.json version [%s] from [%s] was not found"), requested.c_str(), _global_file_path.c_str());
            trace::error(_X("Install the [%s] .NET Core SDK or update [%s] with an installed .NET Core SDK:"), requested.c_str(), _global_file_path.c_str());
        }
        else
        {
            trace::error(_X("A compatible installed .NET Core SDK version [%s] was not found"), requested.c_str());
            trace::error(_X("Install the [%s] .NET Core SDK or create a global.json file with an installed .NET Core SDK:"), requested.c_str());
        }
    }

    if (requested.empty() || !sdk_info::print_all_sdks(dotnet_root, _X("  ")))
    {
        trace::error(_X("  It was not possible to find any installed .NET Core SDKs"));
        trace::error(_X("  Did you mean to run .NET Core SDK commands? Install a .NET Core SDK from:"));
        trace::error(_X("      %s"), DOTNET_CORE_DOWNLOAD_URL);
    }
    return {};
}

sdk_resolver sdk_resolver::from_nearest_global_file(bool allow_prerelease)
{
    pal::string_t cwd;
    if (!pal::getcwd(&cwd))
    {
        trace::verbose(_X("Failed to obtain current working dir"));
        assert(cwd.empty());
    }
    else
    {
        trace::verbose(_X("--- Resolving .NET Core SDK with working dir [%s]"), cwd.c_str());
    }
    return from_nearest_global_file(cwd, allow_prerelease);
}

sdk_resolver sdk_resolver::from_nearest_global_file(const pal::string_t& cwd, bool allow_prerelease)
{
    sdk_resolver resolver{ allow_prerelease };

    if (!resolver.parse_global_file(find_nearest_global_file(cwd)))
    {
        // Fall back to a default SDK resolver
        resolver = sdk_resolver{ allow_prerelease };

        trace::error(
            _X("Ignoring SDK settings in global.json: the latest installed .NET Core SDK (%s prereleases) will be used"),
            resolver.allow_prerelease() ? _X("including") : _X("excluding"));
    }

    // If the requested version is a prerelease, always allow prerelease versions
    if (resolver._requested.is_prerelease())
    {
        resolver._allow_prerelease = true;
    }

    return resolver;
}

sdk_roll_forward_policy sdk_resolver::to_policy(const pal::string_t& name)
{
    if (name == _X("disable"))
    {
        return sdk_roll_forward_policy::disable;
    }

    if (name == _X("patch"))
    {
        return sdk_roll_forward_policy::patch;
    }

    if (name == _X("feature"))
    {
        return sdk_roll_forward_policy::feature;
    }

    if (name == _X("minor"))
    {
        return sdk_roll_forward_policy::minor;
    }

    if (name == _X("major"))
    {
        return sdk_roll_forward_policy::major;
    }

    if (name == _X("latestPatch"))
    {
        return sdk_roll_forward_policy::latest_patch;
    }

    if (name == _X("latestFeature"))
    {
        return sdk_roll_forward_policy::latest_feature;
    }

    if (name == _X("latestMinor"))
    {
        return sdk_roll_forward_policy::latest_minor;
    }

    if (name == _X("latestMajor"))
    {
        return sdk_roll_forward_policy::latest_major;
    }

    return sdk_roll_forward_policy::unsupported;
}

const char* sdk_resolver::to_policy_name(sdk_roll_forward_policy policy)
{
    switch (policy)
    {
        case sdk_roll_forward_policy::unsupported:
            return "unsupported";

        case sdk_roll_forward_policy::disable:
            return "disable";

        case sdk_roll_forward_policy::patch:
            return "patch";

        case sdk_roll_forward_policy::feature:
            return "feature";

        case sdk_roll_forward_policy::minor:
            return "minor";

        case sdk_roll_forward_policy::major:
            return "major";

        case sdk_roll_forward_policy::latest_patch:
            return "latestPatch";

        case sdk_roll_forward_policy::latest_feature:
            return "latestFeature";

        case sdk_roll_forward_policy::latest_minor:
            return "latestMinor";

        case sdk_roll_forward_policy::latest_major:
            return "latestMajor";
    }
}

pal::string_t sdk_resolver::find_nearest_global_file(const pal::string_t& cwd)
{
    if (!cwd.empty())
    {
        for (pal::string_t parent_dir, cur_dir = cwd; true; cur_dir = parent_dir)
        {
            auto file = cur_dir;
            append_path(&file, _X("global.json"));

            trace::verbose(_X("Probing path [%s] for global.json"), file.c_str());
            if (pal::file_exists(file))
            {
                trace::verbose(_X("Found global.json [%s]"), file.c_str());
                return file;
            }
            parent_dir = get_directory(cur_dir);
            if (parent_dir.empty() || parent_dir.size() == cur_dir.size())
            {
                trace::verbose(_X("Terminating global.json search at [%s]"), parent_dir.c_str());
                break;
            }
        }
    }

    return {};
}

bool sdk_resolver::parse_global_file(pal::string_t global_file_path)
{
    if (global_file_path.empty())
    {
        // Missing global.json is treated as success (use default resolver)
        return true;
    }

    trace::verbose(_X("--- Resolving SDK information from global.json [%s]"), global_file_path.c_str());

    pal::ifstream_t file{ global_file_path };
    if (!file.good())
    {
        trace::error(_X("[%s] could not be opened"), global_file_path.c_str());
        return false;
    }

    if (skip_utf8_bom(&file))
    {
        trace::verbose(_X("UTF-8 BOM skipped while reading [%s]"), global_file_path.c_str());
    }

    json_value doc;
    try
    {
        doc = json_value::parse(file);
    }
    catch (const exception& ex)
    {
        pal::string_t msg;
        (void)pal::utf8_palstring(ex.what(), &msg);
        trace::error(_X("A JSON parsing exception occurred in [%s]: %s"), global_file_path.c_str(), msg.c_str());
        return false;
    }

    if (!doc.is_object())
    {
        trace::error(_X("Expected a JSON object in [%s]"), global_file_path.c_str());
        return false;
    }

    const auto& doc_obj = doc.as_object();

    const auto sdk = doc_obj.find(_X("sdk"));
    if (sdk == doc_obj.end() || sdk->second.is_null())
    {
        // Missing SDK is treated as success (use default resolver)
        trace::verbose(_X("Value 'sdk' is missing or null in [%s]"), global_file_path.c_str());
        return true;
    }

    if (!sdk->second.is_object())
    {
        trace::error(_X("Expected a JSON object for the 'sdk' value in [%s]"), global_file_path.c_str());
        return false;
    }

    const auto& sdk_obj = sdk->second.as_object();

    const auto version = sdk_obj.find(_X("version"));
    if (version == sdk_obj.end() || version->second.is_null())
    {
        trace::verbose(_X("Value 'sdk/version' is missing or null in [%s]"), global_file_path.c_str());
    }
    else
    {
        if (!version->second.is_string())
        {
            trace::error(_X("Expected a string for the 'sdk/version' value in [%s]"), global_file_path.c_str());
            return false;
        }

        if (!fx_ver_t::parse(version->second.as_string(), &_requested, false))
        {
            trace::error(
                _X("Version '%s' is not valid for the 'sdk/version' value in [%s]"),
                version->second.as_string().c_str(),
                global_file_path.c_str()
            );
            return false;
        }

        // The default policy when a version is specified is 'patch'
        _policy = sdk_roll_forward_policy::patch;
    }

    const auto roll_forward = sdk_obj.find(_X("rollForward"));
    if (roll_forward == sdk_obj.end() || roll_forward->second.is_null())
    {
        trace::verbose(_X("Value 'sdk/rollForward' is missing or null in [%s]"), global_file_path.c_str());
    }
    else
    {
        if (!roll_forward->second.is_string())
        {
            trace::error(_X("Expected a string for the 'sdk/rollForward' value in [%s]"), global_file_path.c_str());
            return false;
        }

        _policy = to_policy(roll_forward->second.as_string());
        if (_policy == sdk_roll_forward_policy::unsupported)
        {
            trace::error(
                _X("The roll-forward policy '%s' is not supported for the 'sdk/rollForward' value in [%s]"),
                roll_forward->second.as_string().c_str(),
                global_file_path.c_str()
            );
            return false;
        }

        // All policies other than 'latestMajor' require a version to operate
        if (_policy != sdk_roll_forward_policy::latest_major && _requested.is_empty())
        {
            trace::error(
                _X("The roll-forward policy '%s' requires a 'sdk/version' value in [%s]"),
                roll_forward->second.as_string().c_str(),
                global_file_path.c_str()
            );
            return false;
        }
    }

    const auto prerelease = sdk_obj.find(_X("allowPrerelease"));
    if (prerelease == sdk_obj.end() || prerelease->second.is_null())
    {
        trace::verbose(_X("Value 'sdk/allowPrerelease' is missing or null in [%s]"), global_file_path.c_str());
    }
    else
    {
        if (!prerelease->second.is_boolean())
        {
            trace::error(_X("Expected a boolean for the 'sdk/allowPrerelease' value in [%s]"), global_file_path.c_str());
            return false;
        }

        _allow_prerelease = prerelease->second.as_bool();

        if (!_allow_prerelease && _requested.is_prerelease())
        {
            trace::warning(_X("Ignoring the 'sdk/allowPrerelease' value in [%s] because a prerelease version was specified"), global_file_path.c_str());
            _allow_prerelease = true;
        }
    }

    _global_file_path = move(global_file_path);
    return true;
}

bool sdk_resolver::matches_policy(const fx_ver_t& version) const
{
    // Check for unallowed prerelease versions
    if (version.is_empty() || (!_allow_prerelease && version.is_prerelease()))
    {
        return false;
    }

    // If no version was requested, then all versions match
    if (_requested.is_empty())
    {
        return true;
    }

    int requested_patch = _requested.get_patch() % 100;
    int version_patch = version.get_patch() % 100;

    int requested_feature = _requested.get_patch() / 100;
    int version_feature = version.get_patch() / 100;

    int requested_minor = _requested.get_minor();
    int version_minor = version.get_minor();

    int requested_major = _requested.get_major();
    int version_major = version.get_major();

    // First exclude any versions that don't match the policy requirements
    switch (_policy)
    {
        case sdk_roll_forward_policy::unsupported:
        case sdk_roll_forward_policy::disable:
            return false;

        case sdk_roll_forward_policy::patch:
        case sdk_roll_forward_policy::latest_patch:
            if (version_major != requested_major ||
                version_minor != requested_minor ||
                version_feature != requested_feature ||
                version_patch < requested_patch)
            {
                return false;
            }
            break;

        case sdk_roll_forward_policy::feature:
        case sdk_roll_forward_policy::latest_feature:
            if (version_major != requested_major ||
                version_minor != requested_minor ||
                version_feature < requested_feature ||
                (version_feature == requested_feature &&
                 version_patch < requested_patch))
            {
                return false;
            }
            break;

        case sdk_roll_forward_policy::minor:
        case sdk_roll_forward_policy::latest_minor:
            if (version_major != requested_major)
            {
                return false;
            }
            break;

        case sdk_roll_forward_policy::major:
        case sdk_roll_forward_policy::latest_major:
            break;
    }

    // The version must be at least what was requested
    return version >= _requested;
}

bool sdk_resolver::is_better_match(const fx_ver_t& current, const fx_ver_t& previous) const
{
    // Assumption: both current and previous (if there is one) match the policy

    // If no previous match, then the current one is better
    if (previous.is_empty())
    {
        return true;
    }

    // If there wasn't a requested version, then latest is best
    if (_requested.is_empty())
    {
        return current > previous;
    }

    int current_patch = current.get_patch() % 100;
    int previous_patch = previous.get_patch() % 100;

    int current_feature = current.get_patch() / 100;
    int previous_feature = previous.get_patch() / 100;

    int current_minor = current.get_minor();
    int previous_minor = previous.get_minor();

    int current_major = current.get_major();
    int previous_major = previous.get_major();

    bool use_latest = is_policy_use_latest();

    if (current_major == previous_major)
    {
        if (current_minor == previous_minor)
        {
            if (current_feature == previous_feature)
            {
                if (current_patch == previous_patch)
                {
                    // Accept the later of the versions
                    // This will handle stable and prerelease comparisons
                    return current > previous;
                }

                // Latest always wins for patch level
                return current_patch > previous_patch;
            }

            return use_latest ? (current_feature > previous_feature) : (current_feature < previous_feature);
        }

        return use_latest ? (current_minor > previous_minor) : (current_minor < previous_minor);
    }

    return use_latest ? (current_major > previous_major) : (current_major < previous_major);
}

bool sdk_resolver::exact_match_allowed() const
{
    return _policy == sdk_roll_forward_policy::disable ||
           _policy == sdk_roll_forward_policy::patch;
}

bool sdk_resolver::is_policy_use_latest() const
{
    return _policy == sdk_roll_forward_policy::latest_patch ||
           _policy == sdk_roll_forward_policy::latest_feature ||
           _policy == sdk_roll_forward_policy::latest_minor ||
           _policy == sdk_roll_forward_policy::latest_major;
}

bool sdk_resolver::resolve_sdk_path_and_version(const pal::string_t& dir, pal::string_t& sdk_path, fx_ver_t& resolved_version) const
{
    trace::verbose(_X("Searching for SDK versions in [%s]"), dir.c_str());

    // If an exact match is allowed, check for the existence of the version
    if (exact_match_allowed() && !_requested.is_empty())
    {
        auto probe_path = dir;
        append_path(&probe_path, _requested.as_str().c_str());

        if (pal::directory_exists(probe_path))
        {
            trace::verbose(_X("Found requested SDK directory [%s]"), probe_path.c_str());
            sdk_path = move(probe_path);
            resolved_version = _requested;

            // The SDK path has been resolved
            return true;
        }
    }

    if (_policy == sdk_roll_forward_policy::disable)
    {
        // Not yet fully resolved
        return false;
    }

    vector<pal::string_t> versions;
    pal::readdir_onlydirectories(dir, &versions);

    bool changed = false;
    pal::string_t resolved_version_str = resolved_version.is_empty() ? pal::string_t{} : resolved_version.as_str();
    for (auto&& version : versions)
    {
        fx_ver_t ver;
        if (!fx_ver_t::parse(version, &ver, false))
        {
            trace::verbose(_X("Ignoring invalid version [%s]"), version.c_str());
            continue;
        }

        if (!matches_policy(ver))
        {
            trace::verbose(_X("Ignoring version [%s] because it does not match the roll-forward policy"), version.c_str());
            continue;
        }

        if (!is_better_match(ver, resolved_version))
        {
            trace::verbose(
                _X("Ignoring version [%s] because it is not a better match than [%s]"),
                version.c_str(),
                resolved_version_str.empty() ? _X("none") : resolved_version_str.c_str()
            );
            continue;
        }

        trace::verbose(
            _X("Version [%s] is a better match than [%s]"),
            version.c_str(),
            resolved_version_str.empty() ? _X("none") : resolved_version_str.c_str()
        );

        changed = true;
        resolved_version = ver;
        resolved_version_str = move(version);
    }

    if (changed)
    {
        sdk_path = dir;
        append_path(&sdk_path, resolved_version_str.c_str());
    }

    // Not yet fully resolved
    return false;
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>
#include "pal.h"
#include "version.h"

version_t::version_t() : version_t(-1, -1, -1, -1) { }

version_t::version_t(int major, int minor, int build, int revision)
    : m_major(major)
    , m_minor(minor)
    , m_build(build)
    , m_revision(revision) { }

bool version_t::operator ==(const version_t& b) const
{
    return compare(*this, b) == 0;
}

bool version_t::operator !=(const version_t& b) const
{
    return !operator ==(b);
}

bool version_t::operator <(const version_t& b) const
{
    return compare(*this, b) < 0;
}

bool version_t::operator >(const version_t& b) const
{
    return compare(*this, b) > 0;
}

bool version_t::operator <=(const version_t& b) const
{
    return compare(*this, b) <= 0;
}

bool version_t::operator >=(const version_t& b) const
{
    return compare(*this, b) >= 0;
}

pal::string_t version_t::as_str() const
{
    pal::stringstream_t stream;

    if (m_major >= 0)
    {
        stream << m_major;

        if (m_minor >= 0)
        {
            stream << _X(".") << m_minor;

            if (m_build >= 0)
            {
                stream << _X(".") << m_build;

                if (m_revision >= 0)
                {
                    stream << _X(".") << m_revision;
                }
            }
        }
    }

    return stream.str();
}

/*static*/ int version_t::compare(const version_t&a, const version_t& b)
{
    if (a.m_major != b.m_major)
    {
        return (a.m_major > b.m_major) ? 1 : -1;
    }

    if (a.m_minor != b.m_minor)
    {
        return (a.m_minor > b.m_minor) ? 1 : -1;
    }

    if (a.m_build != b.m_build)
    {
        return (a.m_build > b.m_build) ? 1 : -1;
    }

    if (a.m_revision != b.m_revision)
    {
        return (a.m_revision > b.m_revision) ? 1 : -1;
    }

    return 0;
}

bool parse_internal(const pal::string_t& ver, version_t* ver_out)
{
    unsigned major = -1;
    size_t maj_start = 0;
    size_t maj_sep = ver.find(_X('.'));
    if (maj_sep == pal::string_t::npos)
    {
        return false; // minor required
    }
    if (!try_stou(ver.substr(maj_start, maj_sep), &major))
    {
        return false;
    }

    unsigned minor = -1;
    size_t min_start = maj_sep + 1;
    size_t min_sep = ver.find(_X('.'), min_start);
    if (min_sep == pal::string_t::npos)
    {
        if (!try_stou(ver.substr(min_start), &minor))
        {
            return false;
        }
        *ver_out = version_t(major, minor, -1, -1);
        return true; // build and revision not required
    }
    if (!try_stou(ver.substr(min_start, min_sep - min_start), &minor))
    {
        return false;
    }

    unsigned build = -1;
    size_t build_start = min_sep + 1;
    size_t build_sep = ver.find(_X('.'), build_start);
    if (build_sep == pal::string_t::npos)
    {
        if (!try_stou(ver.substr(build_start), &build))
        {
            return false;
        }
        *ver_out = version_t(major, minor, build, -1);
        return true; // revision not required
    }
    if (!try_stou(ver.substr(build_start, build_sep - build_start), &build))
    {
        return false;
    }

    unsigned revision = -1;
    size_t revision_start = build_sep + 1;
    if (!try_stou(ver.substr(revision_start), &revision))
    {
        return false;
    }
    *ver_out = version_t(major, minor, build, revision);

    return true;
}

/* static */
bool version_t::parse(const pal::string_t& ver, version_t* ver_out)
{
    bool valid = parse_internal(ver, ver_out);
    assert(!valid || ver_out->as_str() == ver);
    return valid;
}

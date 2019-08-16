// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "header.h"
#include "reader.h"
#include "error_codes.h"
#include "trace.h"

using namespace bundle;

bool header_t::is_valid()
{
    return m_num_embedded_files > 0 &&
           ((m_major_version < current_major_version) ||
            (m_major_version == current_major_version && m_minor_version <= current_minor_version));
}

header_t header_t::read(reader_t& reader)
{
    const header_fixed_t* fixed_data = reinterpret_cast<const header_fixed_t*>(reader.read_direct(sizeof(header_fixed_t)));
    header_t header(fixed_data);

    if (!header.is_valid())
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Bundle header version compatibility check failed"));

        throw StatusCode::BundleExtractionFailure;
    }

    // bundle_id is a component of the extraction path
    reader.read_path_string(header.m_bundle_id);

    return header;
}

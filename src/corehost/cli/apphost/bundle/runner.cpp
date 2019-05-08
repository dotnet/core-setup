// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "runner.h"
#include "extractor.h"

using namespace bundle;

void runner_t::map_host()
{
    m_bundle_map = (int8_t *) pal::map_file_readonly(m_bundle_path, m_bundle_length);

    if (m_bundle_map == nullptr)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Couldn't memory map the bundle file for reading"));
        throw StatusCode::BundleExtractionIOError;
    }
}

void runner_t::unmap_host()
{
    if (!unmap_file(m_bundle_map, m_bundle_length))
    {
        trace::warning(_X("Failed to unmap bundle after extraction."));
    }
}

// Current support for executing single-file bundles involves 
// extraction of embedded files to actual files on disk. 
// This method implements the file extraction functionality at startup.
StatusCode runner_t::extract()
{
    try
    {
        map_host();
        reader_t reader(m_bundle_map);

        // Read the bundle header
        reader.set_offset(marker_t::header_offset());
        m_header = header_t::read(reader);

        extractor_t extractor(bundle_id(), m_bundle_path);
        m_extraction_path = extractor.extraction_dir();

        // Determine if embedded files are already extracted, and available for reuse
        if (extractor.can_reuse_extraction())
        {
            return StatusCode::Success;
        }

        m_manifest = manifest_t::read(reader, num_embedded_files());

        extractor.begin();
        for (const file_entry_t &entry : m_manifest->files) {
            extractor.extract(entry, reader);
        }
        extractor.commit();

        unmap_host();

        return StatusCode::Success;
    }
    catch (StatusCode e)
    {
        unmap_host();

        return e;
    }
}

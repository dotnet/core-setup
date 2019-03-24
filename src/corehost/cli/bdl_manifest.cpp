// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifdef FEATURE_APPHOST

#include "bdl_processor.h"
#include "pal.h"
#include "error_codes.h"
#include "trace.h"
#include "utils.h"

const char *manifest_footer_t::m_expected_signature = ".NetCoreBundle";

bool manifest_footer_t::is_valid()
{
	return header_offset > 0 &&
		   signature_length == 14 &&
		   strcmp(signature, m_expected_signature) == 0;
}

manifest_footer_t* manifest_footer_t::read(FILE* bundle)
{
	manifest_footer_t* footer = new manifest_footer_t();

	bdl_processor_t::read(footer, num_bytes_read(), bundle);

	if (!footer->is_valid())
	{
		trace::info(_X("Manifest footer Invalid"));
		throw StatusCode::AppHostExeNotBundle;
	}

	return footer;
}

bool manifest_header_t::is_valid()
{
	return data.major_version == m_current_major_version &&
		   data.minor_version == m_current_minor_version &&
		   data.num_embedded_files > 0 &&
		   data.bundle_id_length > 0 && data.bundle_id_length < PATH_MAX;
}

manifest_header_t* manifest_header_t::read(FILE* bundle)
{
	manifest_header_t* header = new manifest_header_t();

	// First read the fixed size portion of the header
	bdl_processor_t::read(&header->data, sizeof(header->data), bundle);
	if (!header->is_valid())
	{
		trace::error(_X("Manifest header incompatible"));
		throw StatusCode::BundleExtractionFailure;
	}
	 
	// Next read the bundle-ID string, given its length
	bdl_processor_t::read_string(header->bundle_id, header->data.bundle_id_length, bundle);

	return header;
}

manifest_t* manifest_t::read(FILE* bundle, int32_t num_files)
{
	manifest_t* manifest = new manifest_t();

	for (int32_t i = 0; i < num_files; i++)
	{
		file_entry_t* entry = file_entry_t::read(bundle);
		if (entry == nullptr)
		{
			return nullptr;
		}

		manifest->files.push_back(entry);
	}

	return manifest;
}

#endif // FEATURE_APPHOST

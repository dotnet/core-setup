// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __BDL_PROCESSOR_H__
#define __BDL_PROCESSOR_H__

#if FEATURE_APPHOST

#include <cstdint>
#include "bdl_manifest.h"
#include "error_codes.h"

//  If the current AppHost is a bundle, it's layout will be 
//    AppHost binary 
//    Embedded Files: including the app, its configuration files, 
//                    dependencies, and possibly the runtime.
//    Bundle Manifest

class bdl_processor_t
{
public:
    bdl_processor_t (const pal::string_t &bundle_path)
        :m_bundle_path(bundle_path),
         m_bundle(nullptr),
         m_manifest(nullptr),
         m_num_embedded_files(0)
    {
    }

	pal::string_t get_extraction_dir()
	{
		return m_extraction_dir;
	}

	StatusCode extract();

	static void read(void* buf, size_t size, FILE* stream);
	static void write(const void* buf, size_t size, FILE* stream);
	static void read_string(pal::string_t& str, size_t size, FILE* stream);

private:
	void reopen_host_for_reading();
	void seek(long offset, int origin);

	void process_manifest_footer(int64_t& header_offset);
	void process_manifest_header(int64_t header_offset);

	void determine_extraction_dir();
	void create_working_extraction_dir();
	bool can_reuse_extraction();

	FILE* create_extraction_file(const pal::string_t& relative_path);
	void extract_file(file_entry_t* entry);
	
	FILE* m_bundle;
	manifest_t* m_manifest;
	int32_t m_num_embedded_files;
	pal::string_t m_bundle_path;
	pal::string_t m_bundle_id;
	pal::string_t m_extraction_dir;
	pal::string_t m_working_extraction_dir;
};

#endif // FEATURE_APPHOST

#endif // __BDL_PROCESSOR_H__

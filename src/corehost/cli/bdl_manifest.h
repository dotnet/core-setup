// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __BDL_MANIFEST_H__
#define __BDL_MANIFEST_H__

#if FEATURE_APPHOST

#include <cstdint>
#include <list>
#include "bdl_file_entry.h"

#pragma pack(push, 1)

// Manifest Header contains:
// Fixed size thunk (represened by manifest_header_inner_t)
//   - Major Version     
//   - Minor Version     
//   - Number of embedded files
//   - Bundle ID length 
// Variable size portion:
//   - Bundle ID ("Bundle ID length" bytes)

struct manifest_header_t
{
public:
	struct manifest_header_inner_t
	{
		uint32_t major_version;
		uint32_t minor_version;
		int32_t num_embedded_files;
		int8_t bundle_id_length;
	} data;
	pal::string_t bundle_id;

	manifest_header_t()
		:data(), bundle_id()
	{
	}

    static manifest_header_t* read(FILE* bundle);

private:
    bool is_valid();

    static const uint32_t m_current_major_version = 0;
    static const uint32_t m_current_minor_version = 1;
};

// Manifest Footer contains:
//   Manifest header offset
//   Length-prefixed non-null terminated Bundle Signature ".NetCoreBundle"
struct manifest_footer_t
{
public:
    int64_t header_offset;
    uint8_t signature_length;
    char signature[15];

    manifest_footer_t()
        :header_offset(0), signature_length(0)
    {
        // The signature string is not null-terminated as read from disk.
        // We add an additional character for null termination
        signature[14] = 0;
    }

    static manifest_footer_t* read(FILE* bundle);

    static size_t num_bytes_read()
    {
        return sizeof(manifest_footer_t) - 1;
    }

private:
    bool is_valid();

    static const char* m_expected_signature; 
};

#pragma pack(pop)

// Bundle Manifest contains:
//     Series of file entries (for each embedded file)

class manifest_t
{
public:
    manifest_t()
        :files()
    {}

    std::list<file_entry_t *> files;

    static manifest_t* read(FILE *host, int32_t num_files);
};

#endif // FEATURE_APPHOST

#endif // __BDL_MANIFEST_H__

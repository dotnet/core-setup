// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __HEADER_H__
#define __HEADER_H__

#include <cstdint>
#include "pal.h"

namespace bundle
{
    // The Bundle Header contains:
    // Fixed size thunk (header_fixed_t)
    //   - Major Version     
    //   - Minor Version     
    //   - Number of embedded files
    // Variable size portion:
    //   - Bundle ID (7-bit extension encoded length prefixed string)

#pragma pack(push, 1)
    struct
    {
        uint32_t major_version;
        uint32_t minor_version;
        int32_t num_embedded_files;
    } header_fixed_t;
#pragma pack(pop)

    struct header_t
    {
    public:
        header_t(const header_fixed_t* fixed_data)
            : m_bundle_id()
        {
            m_major_version = fixed_data->major_version;
            m_minor_version = fixed_data->minor_version;
            m_num_embedded_files = fixed_data->num_embedded_files;
        }

        bool is_valid();
        static header_t read(reader_t& reader);
        const pal::string_t& bundle_id() { return m_bundle_id; }
        int32_t num_embedded_files() { return m_num_embedded_files;  }

    private:
        uint32_t m_major_version;
        uint32_t m_minor_version;
        int32_t m_num_embedded_files;
        pal::string_t m_bundle_id;

        static const uint32_t current_major_version = 1;
        static const uint32_t current_minor_version = 0;
    };
}
#endif // __HEADER_H__

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EXTRACTOR_H__
#define __EXTRACTOR_H__

#include "reader.h"
#include "file_entry.h"

namespace bundle
{
    class extractor_t
    {
    public:
        extractor_t(const pal::string_t &bundle_id, const pal::string_t& bundle_path)
            :m_extraction_path(), m_working_extraction_path()
        {
            m_bundle_id = bundle_id;
            m_bundle_path = bundle_path;
        }

        pal::string_t& extraction_dir();
        bool can_reuse_extraction();
        void begin();
        void extract(const file_entry_t& entry, reader_t& reader);
        void commit();

    private:
        void determine_extraction_dir();
        void determine_working_extraction_dir();

        FILE* create_extraction_file(const pal::string_t& relative_path);

        pal::string_t m_bundle_id;
        pal::string_t m_bundle_path;
        pal::string_t m_extraction_path;
        pal::string_t m_working_extraction_path;
    };
}

#endif // __EXTRACTOR_H__

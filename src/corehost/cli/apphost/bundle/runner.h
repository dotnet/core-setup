// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __RUNNER_H__
#define __RUNNER_H__

 #include "manifest.h"
#include "error_codes.h"

 namespace bundle
{
    class runner_t
    {
    public:
        runner_t(const pal::string_t& bundle_path)
            : m_bundle_stream(nullptr)
            , m_bundle_path(bundle_path)
        {
        }

        pal::string_t get_extraction_dir()
        {
            return m_extraction_dir;
        }

        StatusCode extract();

    private:
        void reopen_host_for_reading();

        int32_t num_embedded_files() { return m_header.num_embedded_files(); }
        const pal::string_t& bundle_id() { return m_header.bundle_id(); }

        FILE* m_bundle_stream;
        header_t m_header;
        manifest_t m_manifest;
        pal::string_t m_bundle_path;
        pal::string_t m_extraction_dir;
        pal::string_t m_working_extraction_dir;
    };

}

#endif // __BUNDLE_RUNNER_H__

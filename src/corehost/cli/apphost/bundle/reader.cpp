// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "reader.h"
#include "error_codes.h"
#include "trace.h"

using namespace bundle;

// Handle the relatively uncommon scenario where the bundle ID or 
// the relative-path of a file within the bundle is longer than 127 bytes
size_t reader_t::read_path_length()
{
    size_t length = 0;

    int8_t first_byte = read();

    // If the high bit is set, it means there are more bytes to read.
    if ((first_byte & 0x80) == 0)
    {
         length = first_byte;
    }
    else
    {
        int8_t second_byte = read();

        if (second_byte & 0x80)
        {
            // There can be no more than two bytes in path_length
            trace::error(_X("Failure processing application bundle; possible file corruption."));
            trace::error(_X("Path length encoding read beyond two bytes."));

            throw StatusCode::BundleExtractionFailure;
        }

        length = (second_byte << 7) | (first_byte & 0x7f);
    }

    if (length <= 0 || length > PATH_MAX)
    {
        trace::error(_X("Failure processing application bundle; possible file corruption."));
        trace::error(_X("Path length is zero or too long."));
        throw StatusCode::BundleExtractionFailure;
    }

    return length;
}

void reader_t::read_path_string(pal::string_t &str)
{
    size_t size = read_path_length();
    std::unique_ptr<uint8_t[]> buffer{ new uint8_t[size + 1] };
    read(buffer.get(), size);
    buffer[size] = 0; // null-terminator
    pal::clr_palstring(reinterpret_cast<const char*>(buffer.get()), &str);
}

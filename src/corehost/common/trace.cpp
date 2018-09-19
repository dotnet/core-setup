// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "trace.h"
#include <mutex>

static bool g_enabled = false;
static FILE * g_trace_file = stderr;
static std::mutex g_trace_mutex;

//
// Turn on tracing for the corehost based on "COREHOST_TRACE" & "COREHOST_TRACEFILE" env.
//
void trace::setup()
{
    // Read trace environment variable
    pal::string_t trace_str;
    if (!pal::getenv(_X("COREHOST_TRACE"), &trace_str))
    {
        return;
    }

    auto trace_val = pal::xtoi(trace_str.c_str());
    if (trace_val > 0)
    {
        if(trace::enable())
        {
            auto ts = pal::get_timestamp();
            trace::info(_X("Tracing enabled @ %s"), ts.c_str());
        }
    }
}

bool trace::enable()
{
    std::lock_guard<std::mutex> lock(g_trace_mutex);

    bool file_open_error = false;
    pal::string_t tracefile_str;

    if (g_enabled)
    {
        return false;
    }
    else
    {
        g_trace_file = stderr;
        if (pal::getenv(_X("COREHOST_TRACEFILE"), &tracefile_str))
        {
            FILE *tracefile = pal::file_open(tracefile_str, _X("a"));

            if (tracefile)
            {
                g_trace_file = tracefile;
            }
            else
            {
                file_open_error = true;
            }
        }
        g_enabled = true;
    }

    if(file_open_error)
    {
        trace::error(_X("Unable to open COREHOST_TRACEFILE=%s for writing"), tracefile_str.c_str());
    }
    return true;
}

bool trace::is_enabled()
{
    return g_enabled;
}

void trace::verbose(const pal::char_t* format, ...)
{
    if (g_enabled)
    {
        std::lock_guard<std::mutex> lock(g_trace_mutex);

        va_list args;
        va_start(args, format);
        pal::file_vprintf(g_trace_file, format, args);
        va_end(args);
    }
}

void trace::info(const pal::char_t* format, ...)
{
    if (g_enabled)
    {
        std::lock_guard<std::mutex> lock(g_trace_mutex);

        va_list args;
        va_start(args, format);
        pal::file_vprintf(g_trace_file, format, args);
        va_end(args);
    }
}

void trace::error(const pal::char_t* format, ...)
{
    std::lock_guard<std::mutex> lock(g_trace_mutex);

    // Always print errors
    va_list args;
    va_start(args, format);
    pal::err_vprintf(format, args);
    if (g_enabled && (g_trace_file != stderr))
    {
        pal::file_vprintf(g_trace_file, format, args);
    }
    va_end(args);
}

void trace::println(const pal::char_t* format, ...)
{
    std::lock_guard<std::mutex> lock(g_trace_mutex);

    va_list args;
    va_start(args, format);
    pal::out_vprintf(format, args);
    va_end(args);
}

void trace::println()
{
    println(_X(""));
}

void trace::warning(const pal::char_t* format, ...)
{
    if (g_enabled)
    {
        std::lock_guard<std::mutex> lock(g_trace_mutex);

        va_list args;
        va_start(args, format);
        pal::file_vprintf(g_trace_file, format, args);
        va_end(args);
    }
}

void trace::flush()
{
    std::lock_guard<std::mutex> lock(g_trace_mutex);

    pal::file_flush(g_trace_file);
    pal::err_flush();
    pal::out_flush();
}

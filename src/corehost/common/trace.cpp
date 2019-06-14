// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "trace.h"
#include <mutex>

// g_trace_verbosity is used to encode COREHOST_TRACE and COREHOST_TRACE_VERBOSITY to selectively control output of
//    TRACE_WARNING(), TRACE_INFO(), and TRACE_VERBOSE()
//  COREHOST_TRACE=0 COREHOST_TRACE_VERBOSITY=N/A        implies g_trace_verbosity = 0.  // Trace "disabled". TRACE_ERROR() messages will be produced.
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=4 or unset implies g_trace_verbosity = 4.  // Trace "enabled".  TRACE_VERBOSE(), TRACE_INFO(), TRACE_WARNING() and TRACE_ERROR() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=3          implies g_trace_verbosity = 3.  // Trace "enabled".  TRACE_INFO(), TRACE_WARNING() and TRACE_ERROR() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=2          implies g_trace_verbosity = 2.  // Trace "enabled".  TRACE_WARNING() and TRACE_ERROR() messages will be produced
//  COREHOST_TRACE=1 COREHOST_TRACE_VERBOSITY=1          implies g_trace_verbosity = 1.  // Trace "enabled".  TRACE_ERROR() messages will be produced

static pal::mutex_t g_trace_mutex;
static FILE *g_trace_file = stderr;
static trace::verbosity g_trace_verbosity = trace::verbosity::Disabled;
thread_local static trace::error_writer_fn g_error_writer = nullptr;

PURE_FUNCTION trace::verbosity trace::current_verbosity()
{
    return g_trace_verbosity;
}

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
        if (trace::enable())
        {
            auto ts = pal::get_timestamp();
            TRACE_INFO(_X("Tracing enabled @ %s"), ts.c_str());
        }
    }
}

bool trace::enable()
{
    bool file_open_error = false;
    pal::string_t tracefile_str;

    if (trace::is_enabled())
    {
        return false;
    }

    g_trace_file = stderr;
    if (pal::getenv(_X("COREHOST_TRACEFILE"), &tracefile_str))
    {
        std::lock_guard<pal::mutex_t> lock(g_trace_mutex);
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

    pal::string_t trace_str;
    if (!pal::getenv(_X("COREHOST_TRACE_VERBOSITY"), &trace_str))
    {
        g_trace_verbosity = trace::verbosity::Verbose;
    }
    else
    {
        auto v = pal::xtoi(trace_str.c_str());

        if (v < 0)
        {
            g_trace_verbosity = trace::verbosity::Error;
        }
        else if (v > static_cast<decltype(v)>(trace::verbosity::Verbose))
        {
            g_trace_verbosity = trace::verbosity::Verbose;
        }
        else
        {
            g_trace_verbosity = static_cast<verbosity>(v);
        }
    }

    if (file_open_error)
    {
        TRACE_ERROR(_X("Unable to open COREHOST_TRACEFILE=%s for writing"), tracefile_str.c_str());
    }

    return true;
}

void trace::trace(const pal::char_t *format, ...)
{
    std::lock_guard<pal::mutex_t> lock(g_trace_mutex);
    va_list ap;

    va_start(ap, format);
    pal::file_vprintf(g_trace_file, format, ap);
    va_end(ap);
}

void trace::trace_error(const pal::char_t* format, ...)
{
    std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

    // Always print errors
    va_list args;
    va_start(args, format);

    va_list trace_args;
    va_copy(trace_args, args);

    va_list dup_args;
    va_copy(dup_args, args);
    int count = pal::str_vprintf(nullptr, 0, format, args) + 1;
    std::vector<pal::char_t> buffer(count);
    pal::str_vprintf(&buffer[0], count, format, dup_args);

    if (g_error_writer == nullptr)
    {
        pal::err_fputs(buffer.data());
    }
    else
    {
        g_error_writer(buffer.data());
    }

#if defined(_WIN32)
    ::OutputDebugStringW(buffer.data());
#endif

    if (trace::is_enabled() && ((g_trace_file != stderr) || g_error_writer != nullptr))
    {
        pal::file_vprintf(g_trace_file, format, trace_args);
    }
    va_end(args);
}

void trace::println(const pal::char_t* format, ...)
{
    std::lock_guard<pal::mutex_t> lock(g_trace_mutex);
    va_list args;

    va_start(args, format);
    pal::out_vprintf(format, args);
    va_end(args);
}

void trace::println()
{
    println(_X(""));
}

void trace::flush()
{
    std::lock_guard<pal::mutex_t> lock(g_trace_mutex);

    pal::file_flush(g_trace_file);
    pal::err_flush();
    pal::out_flush();
}

trace::error_writer_fn trace::set_error_writer(trace::error_writer_fn error_writer)
{
    // No need for locking since g_error_writer is thread local.
    error_writer_fn previous_writer = g_error_writer;
    g_error_writer = error_writer;
    return previous_writer;
}

trace::error_writer_fn trace::get_error_writer()
{
    // No need for locking since g_error_writer is thread local.
    return g_error_writer;
}

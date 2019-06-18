// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef TRACE_H
#define TRACE_H

#include "pal.h"

namespace trace
{
    enum class verbosity {
        Disabled = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4,
    };
    trace::verbosity current_verbosity() PURE_FUNCTION;

    void setup();

    bool enable();
    inline bool is_enabled() { return current_verbosity() != verbosity::Disabled; }

    typedef void (*error_writer_fn)(const pal::char_t* message);

    // Sets a callback which is called whenever error is to be written
    // The setting is per-thread (thread local). If no error writer is set for a given thread
    // the error is written to stderr.
    // The callback is set for the current thread which calls this function.
    // The function returns the previously registered writer for the current thread (or null)
    error_writer_fn set_error_writer(error_writer_fn error_writer);

    // Returns the currently set callback for error writing
    error_writer_fn get_error_writer();

    void trace(const pal::char_t* format, ...) COLD_FUNCTION PRINTF_FUNCTION(1, 2);
    void trace_error(const pal::char_t* format, ...) COLD_FUNCTION PRINTF_FUNCTION(1, 2);

    void println(const pal::char_t* format, ...) COLD_FUNCTION PRINTF_FUNCTION(1, 2);
    void println();
    void flush();
};

// Tracing is performed with a macro so that arguments to trace::trace() and
// trace::trace_error() can be evaluated lazily depending on the current
// verbosity level.
#define TRACE_LAZY_DETAIL(Verbosity, ...)                  \
    {                                                      \
        if (Verbosity > trace::current_verbosity())        \
        {                                                  \
            trace::trace(__VA_ARGS__);                     \
        }                                                  \
    }
#define TRACE_WARNING(Args...) TRACE_LAZY_DETAIL(trace::verbosity::Warning, Args)
#define TRACE_INFO(Args...)    TRACE_LAZY_DETAIL(trace::verbosity::Info, Args)
#define TRACE_VERBOSE(Args...) TRACE_LAZY_DETAIL(trace::verbosity::Verbose, Args)

// Error conditions should be rare, so move the call to the tracing function
// to a lambda that's never inlined and far away from the happy path.  This
// reduces the pressure on the instruction cache, branch predictors, and
// prefetchers.  (This optimization requires compiler support, but is just
// a pass-through otherwise.)
#define TRACE_ERROR(...)                                                 \
    [&]() COLD_FUNCTION NO_INLINE { trace::trace_error(__VA_ARGS__); }()

#endif // TRACE_H

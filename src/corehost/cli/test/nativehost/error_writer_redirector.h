// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <pal.h>

#if defined(_WIN32)
#define ERROR_WRITER_CALLTYPE __cdecl
#else
#define ERROR_WRITER_CALLTYPE
#endif

template<int n>
class error_writer_redirector
{
public:
    typedef void(ERROR_WRITER_CALLTYPE* error_writer_fn) (const pal::char_t* message);
    typedef error_writer_fn(ERROR_WRITER_CALLTYPE* set_error_writer_fn) (error_writer_fn error_writer);

    error_writer_redirector(set_error_writer_fn set_error_writer, const pal::char_t* prefix = nullptr)
        : _set_error_writer(set_error_writer)
    {
        _n = n;
        _prefix = prefix;
        _error_output.clear();
        _previous_writer = _set_error_writer(error_writer);
    }

    ~error_writer_redirector()
    {
        _set_error_writer(_previous_writer);
    }

    bool has_errors()
    {
        return _error_output.tellp() != std::streampos(0);
    }

    const pal::string_t get_errors()
    {
        return _error_output.str();
    }

private:
    static const pal::char_t* _prefix;
    set_error_writer_fn _set_error_writer;
    error_writer_fn _previous_writer;

    static pal::stringstream_t _error_output;
    static void HOSTPOLICY_CALLTYPE error_writer(const pal::char_t* message)
    {
        if (_prefix != nullptr)
            _error_output << _prefix;

        _error_output << message;
    }

    int _n;
};

template<int n> pal::stringstream_t error_writer_redirector<n>::_error_output;
template<int n> const pal::char_t* error_writer_redirector<n>::_prefix = nullptr;

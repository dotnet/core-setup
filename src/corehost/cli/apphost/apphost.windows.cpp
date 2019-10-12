// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "apphost.windows.h"
#include "error_codes.h"
#include "pal.h"
#include "trace.h"
#include "utils.h"

namespace
{
    pal::string_t g_buffered_errors;

    void buffering_trace_writer(const pal::char_t* message)
    {
        // Add to buffer for later use.
        g_buffered_errors.append(message).append(_X("\n"));
        // Also write to stderr immediately
        pal::err_fputs(message);
    }

    // Determines if the current module (apphost executable) is marked as a Windows GUI application
    bool is_gui_application()
    {
        HMODULE module = ::GetModuleHandleW(nullptr);
        assert(module != nullptr);

        // https://docs.microsoft.com/en-us/windows/win32/debug/pe-format
        BYTE *bytes = reinterpret_cast<BYTE *>(module);
        UINT32 pe_header_offset = reinterpret_cast<IMAGE_DOS_HEADER *>(bytes)->e_lfanew;
        UINT16 subsystem = reinterpret_cast<IMAGE_NT_HEADERS *>(bytes + pe_header_offset)->OptionalHeader.Subsystem;

        return subsystem == IMAGE_SUBSYSTEM_WINDOWS_GUI;
    }

    void write_errors_to_event_log(const pal::char_t *executable_path, const pal::char_t *executable_name)
    {
        // Report errors to the Windows Event Log.
        auto eventSource = ::RegisterEventSourceW(nullptr, _X(".NET Runtime"));
        const DWORD traceErrorID = 1023; // Matches CoreCLR ERT_UnmanagedFailFast
        pal::string_t message;
        message.append(_X("Description: A .NET Core application failed.\n"));
        message.append(_X("Application: ")).append(executable_name).append(_X("\n"));
        message.append(_X("Path: ")).append(executable_path).append(_X("\n"));
        message.append(_X("Message: ")).append(g_buffered_errors).append(_X("\n"));

        LPCWSTR messages[] = {message.c_str()};
        ::ReportEventW(eventSource, EVENTLOG_ERROR_TYPE, 0, traceErrorID, nullptr, 1, 0, messages, nullptr);
        ::DeregisterEventSource(eventSource);
    }

    void show_error_dialog(const pal::char_t *executable_name, int error_code)
    {
        // Show message dialog for UI apps with actionable errors
        if (error_code != StatusCode::CoreHostLibMissingFailure  // missing hostfxr
            && error_code != StatusCode::FrameworkMissingFailure) // missing framework
            return;

        pal::string_t gui_errors_disabled;
        if (pal::getenv(_X("DOTNET_DISABLE_GUI_ERRORS"), &gui_errors_disabled) && pal::xtoi(gui_errors_disabled.c_str()) == 1)
            return;

        pal::string_t dialogMsg = _X("To run this application, you must install .NET Core.\n\n");
        pal::string_t url = DOTNET_CORE_DOWNLOAD_URL;
        if (error_code == StatusCode::CoreHostLibMissingFailure)
        {
            url.append(_X("?missing_runtime=true"));
        }
        else if (error_code == StatusCode::FrameworkMissingFailure)
        {
            pal::string_t name;
            pal::string_t version;

            // We don't have a great way of passing out different kinds of detailed error info across components, so
            // just match the expected error string. See fx_resolver.messages.cpp.
            pal::string_t line;
            pal::stringstream_t ss(g_buffered_errors);
            while (std::getline(ss, line, _X('\n'))){
                const pal::string_t prefix = _X("The specified framework '");
                const pal::string_t suffix = _X("' was not found.");
                if (starts_with(line, prefix, true) && ends_with(line, suffix, true))
                {
                    pal::string_t framework_info = line.substr(prefix.length(), line.length() - suffix.length() - prefix.length());
                    const pal::string_t version_prefix = _X("', version '");
                    size_t pos = framework_info.find(version_prefix);
                    if (pos != pal::string_t::npos)
                    {
                        name = framework_info.substr(0, pos);
                        version = framework_info.substr(pos + version_prefix.length(), framework_info.length() - pos - version_prefix.length());
                    }
                    else
                    {
                        name = framework_info;
                    }

                    dialogMsg.append(_X("The framework '"));
                    dialogMsg.append(name);
                    if (!version.empty())
                    {
                        dialogMsg.append(_X("', version '"));
                        dialogMsg.append(version);
                    }
                    dialogMsg.append(_X("' was not found.\n\n"));

                    break;
                }
            }

            assert(!name.empty());
            url.append(_X("?framework="));
            url.append(name);
            if (!version.empty())
            {
                url.append(_X("&version="));
                url.append(version);
            }
        }

        dialogMsg.append(_X("Would you like to download it now?"));
        url.append(_X("&arch="));
        url.append(get_arch());
        pal::string_t rid = get_current_runtime_id(true /*use_fallback*/);
        url.append(_X("&rid="));
        url.append(rid);

        trace::verbose(_X("Showing error dialog for application: '%s' - error code: 0x%x - url: '%s'"), executable_name, error_code, url.c_str());
        if (::MessageBoxW(nullptr, dialogMsg.c_str(), executable_name, MB_ICONERROR | MB_YESNO) == IDYES)
        {
            // Open the URL in default browser
            ::ShellExecuteW(
                nullptr,
                _X("open"),
                url.c_str(),
                nullptr,
                nullptr,
                SW_SHOWNORMAL);
        }
    }
}

void apphost::buffer_errors()
{
    trace::verbose(_X("Redirecting errors to custom writer."));
    trace::set_error_writer(buffering_trace_writer);
}

void apphost::write_buffered_errors(int error_code)
{
    if (g_buffered_errors.empty())
        return;

    pal::string_t executable_path;
    pal::string_t executable_name;
    if (pal::get_own_executable_path(&executable_path))
    {
        executable_name = get_filename(executable_path);
    }

    write_errors_to_event_log(executable_path.c_str(), executable_name.c_str());

    if (is_gui_application())
        show_error_dialog(executable_name.c_str(), error_code);
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef _COREHOST_CLI_CORECLR_H_
#define _COREHOST_CLI_CORECLR_H_

#include "pal.h"
#include "trace.h"
#include <cstdint>
#include <memory>

class coreclr
{
public: // static
    static pal::hresult_t create(
        const pal::string_t& libcoreclr_path,
        const char* exe_path,
        const char* app_domain_friendly_name,
        int property_count,
        const char** property_keys,
        const char** property_values,
        std::unique_ptr<coreclr> &inst);

public:
    using host_handle_t = void*;
    using domain_id_t = std::uint32_t;

    coreclr(host_handle_t host_handle, domain_id_t domain_id);
    ~coreclr();

    pal::hresult_t execute_assembly(
        int argc,
        const char** argv,
        const char* managed_assembly_path,
        unsigned int* exit_code);

    pal::hresult_t shutdown(int* latchedExitCode);

private:
    host_handle_t _host_handle;
    domain_id_t _domain_id;
};

#endif // _COREHOST_CLI_CORECLR_H_

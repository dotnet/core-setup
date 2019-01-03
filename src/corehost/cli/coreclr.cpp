// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <cassert>

#include "coreclr.h"
#include "utils.h"
#include "error_codes.h"

// Prototype of the coreclr_initialize function from coreclr.dll
using coreclr_initialize_fn = pal::hresult_t(STDMETHODCALLTYPE *)(
    const char* exePath,
    const char* appDomainFriendlyName,
    int propertyCount,
    const char** propertyKeys,
    const char** propertyValues,
    coreclr::host_handle_t* hostHandle,
    unsigned int* domainId);

// Prototype of the coreclr_shutdown function from coreclr.dll
using coreclr_shutdown_fn = pal::hresult_t(STDMETHODCALLTYPE *)(
    coreclr::host_handle_t hostHandle,
    unsigned int domainId,
    int* latchedExitCode);

// Prototype of the coreclr_execute_assembly function from coreclr.dll
using coreclr_execute_assembly_fn = pal::hresult_t(STDMETHODCALLTYPE *)(
    coreclr::host_handle_t hostHandle,
    unsigned int domainId,
    int argc,
    const char** argv,
    const char* managedAssemblyPath,
    unsigned int* exitCode);

namespace
{
    pal::dll_t g_coreclr = nullptr;
    coreclr_shutdown_fn coreclr_shutdown = nullptr;
    coreclr_initialize_fn coreclr_initialize = nullptr;
    coreclr_execute_assembly_fn coreclr_execute_assembly = nullptr;

    bool coreclr_bind(const pal::string_t& libcoreclr_path)
    {
        assert(g_coreclr == nullptr);

        pal::string_t coreclr_dll_path(libcoreclr_path);
        append_path(&coreclr_dll_path, LIBCORECLR_NAME);

        if (!pal::load_library(&coreclr_dll_path, &g_coreclr))
        {
            return false;
        }

        coreclr_initialize = (coreclr_initialize_fn)pal::get_symbol(g_coreclr, "coreclr_initialize");
        coreclr_shutdown = (coreclr_shutdown_fn)pal::get_symbol(g_coreclr, "coreclr_shutdown_2");
        coreclr_execute_assembly = (coreclr_execute_assembly_fn)pal::get_symbol(g_coreclr, "coreclr_execute_assembly");

        return true;
    }
    
    void coreclr_unload()
    {
        assert(g_coreclr != nullptr && coreclr_initialize != nullptr);

        pal::unload_library(g_coreclr);
    }
}

pal::hresult_t coreclr::create(
    const pal::string_t& libcoreclr_path,
    const char* exe_path,
    const char* app_domain_friendly_name,
    int property_count,
    const char** property_keys,
    const char** property_values,
    std::unique_ptr<coreclr> &inst)
{
    if (!coreclr_bind(libcoreclr_path))
    {
        trace::error(_X("Failed to bind to CoreCLR at '%s'"), libcoreclr_path.c_str());
        return StatusCode::CoreClrBindFailure;
    }

    assert(g_coreclr != nullptr && coreclr_initialize != nullptr);

    host_handle_t host_handle;
    domain_id_t domain_id;

    pal::hresult_t hr;
    hr = coreclr_initialize(
        exe_path,
        app_domain_friendly_name,
        property_count,
        property_keys,
        property_values,
        &host_handle,
        &domain_id);

    if (!SUCCEEDED(hr))
        return hr;

    inst = std::make_unique<coreclr>(host_handle, domain_id);
    return StatusCode::Success;
}

coreclr::coreclr(host_handle_t host_handle, domain_id_t domain_id)
    : _host_handle{ host_handle }
    , _domain_id{ domain_id }
{
}

coreclr::~coreclr()
{
    (void)shutdown(nullptr);
    coreclr_unload();
}

pal::hresult_t coreclr::execute_assembly(
    int argc,
    const char** argv,
    const char* managed_assembly_path,
    unsigned int* exit_code)
{
    assert(g_coreclr != nullptr && coreclr_execute_assembly != nullptr);

    return coreclr_execute_assembly(
        _host_handle,
        _domain_id,
        argc,
        argv,
        managed_assembly_path,
        exit_code);
}

pal::hresult_t coreclr::shutdown(int* latchedExitCode)
{
    assert(g_coreclr != nullptr && coreclr_shutdown != nullptr);

    return coreclr_shutdown(_host_handle, _domain_id, latchedExitCode);
}

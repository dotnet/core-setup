// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "mockcoreclr.h"
#include <iostream>
#include "trace.h"

extern "C" pal::hresult_t STDMETHODCALLTYPE coreclr_initialize(
    const char* exePath,
    const char* appDomainFriendlyName,
    int propertyCount,
    const char** propertyKeys,
    const char** propertyValues,
    coreclr_t::host_handle_t* hostHandle,
    unsigned int* domainId)
{
    std::cout << "MockCoreClr::coreclr_initialize(" <<
        "exePath:" << exePath << ", " <<
        "appDomainFriendlyName:" << appDomainFriendlyName << ", " <<
        "propertyCount:" << propertyCount << ", " <<
        "propertyKeys:" << propertyKeys << ", " <<
        "propertyValues:" << propertyValues << ", " <<
        "hostHandle:" << hostHandle << ", " <<
        "domainId:" << domainId << ")" << std::endl;

    for (int i = 0; i < propertyCount; ++i)
    {
        std::cout << "MockCoreClr::coreclr_initialize.property[" << propertyKeys[i] << "] = " << propertyValues[i] << std::endl;
    }

    if (hostHandle != nullptr)
    {
        *hostHandle = (coreclr_t::host_handle_t*) 0xdeadbeef;
    }

    return StatusCode::Success;
}


// Prototype of the coreclr_shutdown function from coreclr.dll
extern "C" pal::hresult_t STDMETHODCALLTYPE coreclr_shutdown_2(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    int* latchedExitCode)
{
    std::cout << "MockCoreClr::coreclr_shutdown_2(" <<
        "hostHandle:" << hostHandle << ", " <<
        "domainId:" << domainId << ")" << std::endl;

    if (latchedExitCode != nullptr)
    {
        *latchedExitCode = 0;
    }

    return StatusCode::Success;
}

// Prototype of the coreclr_execute_assembly function from coreclr.dll
extern "C" pal::hresult_t STDMETHODCALLTYPE coreclr_execute_assembly(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    int argc,
    const char** argv,
    const char* managedAssemblyPath,
    unsigned int* exitCode)
{
    std::cout << "MockCoreClr::coreclr_execute_assembly(" <<
        "hostHandle:" << hostHandle << ", " <<
        "domainId:" << domainId << ", " <<
        "argc:" << argc << ", " <<
        "argv:" << argv << ", " <<
        "managedAssemblyPath:" << managedAssemblyPath << ")" << std::endl;

    for (int i = 0; i < argc; ++i)
    {
        std::cout << "MockCoreClr::coreclr_execute_assembly.argv[" << i << "] = " << argv[i] << std::endl;
    }

    if (exitCode != nullptr)
    {
        *exitCode = 0;
    }

    return StatusCode::Success;
}

struct MockCoreClrDelegate
{
    MockCoreClrDelegate() {}
    MockCoreClrDelegate(coreclr_t::host_handle_t hostHandle,
                        unsigned int domainId,
                        const char* entryPointAssemblyName,
                        const char* entryPointTypeName,
                        const char* entryPointMethodName) :
    m_hostHandle(hostHandle),
    m_domainId(domainId),
    m_entryPointAssemblyName(entryPointAssemblyName),
    m_entryPointTypeName(entryPointTypeName),
    m_entryPointMethodName(entryPointMethodName),
    initialized(true)
    {}

    coreclr_t::host_handle_t m_hostHandle;
    unsigned int             m_domainId;
    std::string              m_entryPointAssemblyName;
    std::string              m_entryPointTypeName;
    std::string              m_entryPointMethodName;
    bool initialized;

    void Echo()
    {
        if (!initialized)
        {
            std::cout << "Called MockCoreClrDelegate() ERROR unitialized delegate!!!";
            return;
        }

        std::cout << "Called MockCoreClrDelegate(" <<
            "hostHandle:" << m_hostHandle << ", " <<
            "domainId:" << m_domainId << ", " <<
            "m_entryPointAssemblyName:" << m_entryPointAssemblyName << ", " <<
            "m_entryPointTypeName:" << m_entryPointTypeName << ", " <<
            "m_entryPointMethodName:" << m_entryPointMethodName << ") " << std::endl;
    }
};

typedef void (*CoreClrDelegate)();

const int MaxDelegates = 16;

MockCoreClrDelegate DelegateState[MaxDelegates];

#define DelegateFunction(index)\
void Delegate_ ## index() { DelegateState[index].Echo(); }

DelegateFunction(0);
DelegateFunction(1);
DelegateFunction(2);
DelegateFunction(3);
DelegateFunction(4);
DelegateFunction(5);
DelegateFunction(6);
DelegateFunction(7);
DelegateFunction(8);
DelegateFunction(9);
DelegateFunction(10);
DelegateFunction(11);
DelegateFunction(12);
DelegateFunction(13);
DelegateFunction(14);
DelegateFunction(15);

#undef DelegateFunction

// Prototype of the coreclr_create_delegate function from coreclr.dll
extern "C" pal::hresult_t STDMETHODCALLTYPE coreclr_create_delegate(
    coreclr_t::host_handle_t hostHandle,
    unsigned int domainId,
    const char* entryPointAssemblyName,
    const char* entryPointTypeName,
    const char* entryPointMethodName,
    void** delegate)
{
    std::cout << "MockCoreClr::coreclr_create_delegate(" <<
        "hostHandle:" << hostHandle << ", " <<
        "domainId:" << domainId << ", " <<
        "entryPointAssemblyName:" << entryPointAssemblyName << ", " <<
        "entryPointTypeName:" << entryPointTypeName << ", " <<
        "entryPointMethodName:" << entryPointMethodName << ", " <<
        "delegate:" << delegate << ")" << std::endl;

    static int nextDelegate = 0;
    static CoreClrDelegate delegates[] =
    {
        Delegate_0,
        Delegate_1,
        Delegate_2,
        Delegate_3,
        Delegate_4,
        Delegate_5,
        Delegate_6,
        Delegate_7,
        Delegate_8,
        Delegate_9,
        Delegate_10,
        Delegate_11,
        Delegate_12,
        Delegate_13,
        Delegate_14,
        Delegate_15
    };

    int delegateIndex = (nextDelegate++);

    while (delegateIndex >= MaxDelegates)
    {
        delegateIndex -= MaxDelegates;
        std::cout << "MockCoreClr::coreclr_create_delegate MaxDelegates exceeded recycling older ones" << std::endl;
    }

    MockCoreClrDelegate delegateState(hostHandle, domainId, entryPointAssemblyName, entryPointTypeName, entryPointMethodName);

    DelegateState[delegateIndex] = delegateState;

    *delegate = (void*) delegates[delegateIndex];

    return StatusCode::Success;
}

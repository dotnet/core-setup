// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <iostream>
#include <pal.h>
#include <nethost.h>

namespace
{
    pal::string_t fxr_path;
    void hostfxr_path_callback(const char_t *hostfxr_path)
    {
        fxr_path.assign(hostfxr_path);
    }

    std::vector<char> tostr(const pal::string_t &value)
    {
        std::vector<char> vect;
        pal::pal_utf8string(value, &vect);
        return vect;
    }
}

#if defined(_WIN32)
int __cdecl wmain(const int argc, const pal::char_t *argv[])
#else
int main(const int argc, const pal::char_t *argv[])
#endif
{
    if (argc < 2)
    {
        std::cerr << "Invalid arguments" << std::endl;
        return -1;
    }

    const pal::char_t *command = argv[1];
    if (pal::strcmp(command, _X("nethost_get_hostfxr_path")) == 0)
    {
        const pal::char_t *assembly_path = nullptr;
        if (argc >= 3)
            assembly_path = argv[2];

#if defined(_WIN32)
        pal::string_t testOverride;
        if (pal::getenv(_X("TEST_OVERRIDE_PROGRAMFILES"), &testOverride))
        {
            std::cout << tostr(testOverride).data() << std::endl;
            ::SetEnvironmentVariableW(_X("ProgramFiles"), testOverride.c_str());
            ::SetEnvironmentVariableW(_X("ProgramFiles(x86)"), testOverride.c_str());
        }
#endif

        int res = nethost_get_hostfxr_path(hostfxr_path_callback, assembly_path);
        if (res == 0)
        {
            std::cout << "nethost_get_hostfxr_path succeeded" << std::endl;
            std::cout << "hostfxr_path: " << tostr(pal::to_lower(fxr_path)).data() << std::endl;
        }
        else
        {
            std::cout << "nethost_get_hostfxr_path failed: " << std::hex << std::showbase << res << std::endl;
        }

        return res;
    }
    else
    {
        std::cerr << "Invalid arguments" << std::endl;
        return -1;
    }

    return 0;
}
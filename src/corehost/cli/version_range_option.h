// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __VERSION_RANGE_OPTION_H_
#define __VERSION_RANGE_OPTION_H_

// Defines teh allowed range of versions to consider during roll-forward search
enum class version_range_option
{
    exact = 0,  // Only the specified version is allowed
    patch = 1,  // Any equal or higher version with the same major.minor
    minor = 2,  // Any equal or higher version with the same major
    major = 3,  // Any equal or higher version

    __last      // Sentinel value
};

pal::string_t version_range_option_to_string(version_range_option value);

#endif // __VERSION_RANGE_OPTION_H_

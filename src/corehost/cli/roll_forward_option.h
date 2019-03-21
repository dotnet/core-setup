// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __ROLL_FORWARD_OPTION_H_
#define __ROLL_FORWARD_OPTION_H_

// Specifies the roll forward option value
enum class roll_forward_option
{
    // The order is in increasing level of relaxation
    // Lower values are more restrictive than higher values

    Disabled = 0,    // No roll-forward is allowed - only exact match
    LatestPatch = 1, // Roll forward to latest patch.
    Minor = 2,       // Roll forward to closest minor but same major and then highest patch
    LatestMinor = 3, // Roll forward to highest minor.patch but same major
    Major = 4,       // Roll forward to closest major.minor and then highest patch
    LatestMajor = 5, // Roll forward to highest major.minor.patch
};

#endif // __ROLL_FORWARD_OPTION_H_

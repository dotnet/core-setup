// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef IJW_BOOTSTRAP_THUNK_H
#define IJW_BOOTSTRAP_THUNK_H

#if !defined(_TARGET_AMD64_)
#error "This file should only be included on amd64 builds."
#endif

#include "pal.h"
#include "corhdr.h"

extern "C" void start_runtime_thunk_stub();

#include <pshpack1.h>
class bootstrap_thunk
{
private:
    // 49 BA 78 56 34 12 78 56 34 12 mov         r10,1234567812345678h
    // 49 BB 34 12 34 12 34 12 34 12 mov         r11,1234123412341234h
    // 41 FF E3                      jmp         r11

    static BYTE             s_mov_r10[2];
    static BYTE             s_mov_r11[2];
    static BYTE             s_jmp_r11[3];

    BYTE                    m_mov_r10[2];
    BYTE                    m_val_r10[8];
    BYTE                    m_mov_r11[2];
    BYTE                    m_val_r11[8];
    BYTE                    m_jmp_r11[3];   // total 23 bytes
    BYTE                    m_padding[1];   // 1 byte to pad to 24
    // Data for the thunk
    std::uint32_t           m_token;                        // 4 bytes
    enum {
        e_TOKEN_IS_DEF = 0x1
    };
    UINT32                  m_flags;                        // 4 bytes

    pal::dll_t               m_dll;            // pal::dll_t of this module
                                                    // 8 bytes
    std::uintptr_t               *m_slot;             // VTable slot for this thunk
                                                    // 8 bytes
public:
    // Get thunk from the return address that the call instruction would have pushed
    static bootstrap_thunk *get_thunk_from_cookie(std::uintptr_t cookie);

    // Get thunk from the return address that the call instruction would have pushed
    static bootstrap_thunk *get_thunk_from_entrypoint(std::uintptr_t entryAddr);

    // Initializes the thunk to point to pThunkInitFcn that will load the
    // runtime and perform the real thunk initialization.
    void initialize(std::uintptr_t pThunkInitFcn,
                    pal::dll_t dll,
                    std::uint32_t token,
                    std::uintptr_t *pSlot);

    // Returns the slot address of the vtable entry for this thunk
    std::uintptr_t *get_slot_address();

    // Returns the pal::dll_t for this thunk's module
    pal::dll_t get_dll_handle();

    // Returns the token of this thunk
    std::uint32_t get_token();

    std::uintptr_t get_entrypoint();
};
#include <poppack.h>

#endif
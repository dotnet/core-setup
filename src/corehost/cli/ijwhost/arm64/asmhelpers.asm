; Copyright (c) .NET Foundation and contributors. All rights reserved.
; Licensed under the MIT license. See LICENSE file in the project root for full license information.
;

#include "ksarm64.h"

    TEXTAREA

    EXTERN VTableBootstrapThunkInitHelper

    ;; Common code called from a VTableBootstrapThunk to call VTableBootstrapThunkInitHelper and obtain the
    ;; real target address to which to tail call.
    ;;
    ;; On entry:
    ;;  x16     : parameter provided by the thunk that points back into the thunk itself
    ;;  other argument registers and possibly stack locations set up ready to make the real call
    ;;
    ;; On exit:
    ;;  tail calls to the real target method
    ;;
    NESTED_ENTRY VTableBootstrapThunkInitHelperStub
    
    PROLOG_SAVE_REG_PAIR fp, lr, #-(8*8 + 8*16 + 16)!   ; save frame chain and allocate stack
    stp     x0, x1, [sp, #(16 + 0*8)]                   ; save parameter registers
    stp     x2, x3, [sp, #(16 + 2*8)]
    stp     x4, x5, [sp, #(16 + 4*8)]
    stp     x6, x7, [sp, #(16 + 6*8)]
    stp     q0, q1, [sp, #(16 + 8*8 + 0*16)]
    stp     q2, q3, [sp, #(16 + 8*8 + 2*16)]
    stp     q4, q5, [sp, #(16 + 8*8 + 4*16)]
    stp     q6, q7, [sp, #(16 + 8*8 + 6*16)]

    mov     x0, x16     ; Only argument to VTableBootstrapThunkInitHelper is the hidden thunk parameter
    bl      VTableBootstrapThunkInitHelper

    mov     x16, x0     ; Preserve result (real target address)

    ldp     q6, q7, [sp, #(16 + 8*8 + 6*16)]
    ldp     q4, q5, [sp, #(16 + 8*8 + 4*16)]
    ldp     q2, q3, [sp, #(16 + 8*8 + 2*16)]
    ldp     q0, q1, [sp, #(16 + 8*8 + 0*16)]
    ldp     x6, x7, [sp, #(16 + 6*8)]
    ldp     x4, x5, [sp, #(16 + 4*8)]
    ldp     x2, x3, [sp, #(16 + 2*8)]
    ldp     x0, x1, [sp, #(16 + 0*8)]                   ; save parameter registers
    EPILOG_RESTORE_REG_PAIR fp, lr, #(8*8 + 8*16 + 16)! ; save frame chain and allocate stack
    EPILOG_NOP br x16           ; Tail call to real target

    NESTED_END

    END

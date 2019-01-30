; ==++==
; 
;  Copyright (c) Microsoft Corporation.  All rights reserved.
; 
; ==--== 
;

#include "ksarm.h"

    TEXTAREA

    EXTERN VTableBootstrapThunkInitHelper

    ;; Common code called from a VTableBootstrapThunk to call VTableBootstrapThunkInitHelper and obtain the
    ;; real target address to which to tail call.
    ;;
    ;; On entry:
    ;;  r12     : parameter provided by the thunk that points back into the thunk itself
    ;;  other argument registers and possibly stack locations set up ready to make the real call
    ;;
    ;; On exit:
    ;;  tail calls to the real target method
    ;;
    CFG_ALIGN
    NESTED_ENTRY VTableBootstrapThunkInitHelperStub

    PROLOG_PUSH     {r0-r3}     ; Save general argument registers
    PROLOG_PUSH     {r4,lr}     ; Save return address (r4 is saved simply to preserve stack alignment)
    PROLOG_VPUSH    {d0-d7}     ; Save floating point argument registers

    mov             r0, r12     ; Only argument to VTableBootstrapThunkInitHelper is the hidden thunk parameter
    bl              VTableBootstrapThunkInitHelper

    mov             r12, r0     ; Preserve result (real target address)

    EPILOG_VPOP     {d0-d7}     ; Restore floating point argument registers
    EPILOG_POP      {r4,lr}     ; Restore return address
    EPILOG_POP      {r0-r3}     ; Restore general argument registers

    EPILOG_BRANCH_REG r12       ; Tail call to real target

    NESTED_END

    END

; Copyright (c) .NET Foundation and contributors. All rights reserved.
; Licensed under the MIT license. See LICENSE file in the project root for full license information.


        .586
        .model  flat

        include callconv.inc

        option  casemap:none
        .code

EXTERN _VTableBootstrapThunkInitHelper@4:PROC

AlignCfgProc
_VTableBootstrapThunkInitHelperStub@0 proc public
    ; Stack on entry:
    ;      top->   vtfixup thunk return address
    ;              Unmanaged caller return address

    ; The idea here is similar to the prepad of the MethodDesc, in that we're
    ; using the return address of the call in the stub as a pointer to the
    ; VTableBootstrapThunk struct.

    pop     eax                         ; VTableBootstrapThunk*
    
    push    ebp                         ; Set up EBP frame
    mov     ebp,esp
    
    push    ecx                         ; Save caller registers
    push    edx
    
    push    eax                         ; Push the struct arg
    call    _VTableBootstrapThunkInitHelper@4
    
    pop     edx                         ; Restore the registers
    pop     ecx
    
    pop     ebp                         ; Tear down the EBP frame
    push    eax                         ; Instead of "jmp eax", do "push eax; ret"
    ret                                 ; This keeps the call-return count balanced
_VTableBootstrapThunkInitHelperStub@0 endp

end

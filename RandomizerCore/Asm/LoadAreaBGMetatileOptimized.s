.reloc
LoadAreaBGMetatile:
; guarantee the code is in a000 or higher otherwise we would switch this bank out from under itself
.assert * > $a000
    lda CurrentPrgBank
    asl a
    ora #$80
    sta NmiBankShadow8
    sta PrgBank8Reg
    ; resolve the left/right half-metatile offset outside of the loop
    lda SideviewColumnPhase ; $0740
    and #$01
    eor #$01
    asl a
    sta zp_02 ; store calculated left/right bit
@Loop:
        ; Store the loop counter back into $01
        stx zp_01
        ; Load background tile id from the column buffer high bits
        ; Transform it as follows to get the attribute and metatile
        ; xx000000 -> attr and xx000000 -> 00000xx0 -> tileid
        lda BackgroundColumnUpdateArray,x ; $0464
        and #$c0
        sta zp_03
        asl a
        rol a
        rol a
        asl a
        tay
        ; Read from the fixed address for the backgrounds of the "current" banks
        lda $8500,y
        sta zp_0e
        lda $8501,y
        sta zp_0f
        ; Write 00xxxx00 as 0000xxxx into $02
        lda BackgroundColumnUpdateArray,x
        and #$3f
        asl a
        asl a
        ora zp_02
        ; Use as the offset from the pointer to read the actual tile from the metatile
        tay
        ldx zp_00
        lda (zp_0e),y
        sta PpuBuffer3Data + 3,x ; $03a7
        iny
        lda (zp_0e),y
        sta PpuBuffer3Data + 4,x ; $03a8
        ; $03 is the attribute in the format xx000000
        ; $04 is set to 0 before entering
        ldy zp_04
        ; $05 is set by the caller
        ; Branch if we are loading the attributes for the right side
        lda zp_05
        bne @LoadRightAttribute
@LoadLeftAttribute:
        ; Every other attribute (when the loop counter $01 is odd)
        ; moves to the next byte in the attribute buffer
        lda zp_01
        lsr a
        bcs @BottomLeftAttribute
@TopLeftAttribute:
        rol zp_03
        rol zp_03
        rol zp_03
        jmp @WriteAttribute
@LoadRightAttribute:
        ; Every other attribute (when the loop counter $01 is odd)
        ; moves to the next byte in the attribute buffer
        lda zp_01
        lsr a
        bcs @BottomRightAttribute
@TopRightAttribute:
        lsr zp_03
        lsr zp_03
        lsr zp_03
        lsr zp_03
        jmp @WriteAttribute
@BottomLeftAttribute:
        lsr zp_03
        lsr zp_03
        ; fallthrough to move to the next attribute byte in the buffer
        ; The bottom right attribute doesn't need shifting
@BottomRightAttribute:
        inc zp_04
        ; Write attribute data stored in $03 to the attribute buffer
@WriteAttribute:
        lda $0471,y
        ora zp_03
        sta $0471,y
        inc zp_00
        inc zp_00
        ldx zp_01
        inx
        cpx #$0d
        bcc @Loop
    ; switch back to 0 just in case
    jmp SwapToPRG0

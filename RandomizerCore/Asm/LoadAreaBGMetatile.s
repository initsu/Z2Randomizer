.reloc
LoadAreaBGMetatile:
; guarantee the code is in a000 or higher otherwise we would switch this bank out from under itself
.assert * > $a000
    lda CurrentPrgBank
    asl a
    ora #$80
    sta NmiBankShadow8
    sta PrgBank8Reg
    ; Store the loop counter back into $01
@Loop:
        stx $01
        ; Load background tile id from the column buffer high bits
        ; Transform it as follows to get the attribute and metatile
        ; xx000000 -> attr and xx000000 -> 00000xx0 -> tileid
        lda $0464,x
        and #$c0
        sta $03
        asl a
        rol a
        rol a
        asl a
        tay
        ; Read from the fixed address for the backgrounds of the "current" banks
        lda $8500,y
        sta $0e
        lda $8501,y
        sta $0f
        ; Write 00xxxx00 as 0000xxxx into $02
        lda $0464,x
        and #$3f
        asl a
        asl a
        sta $02
        ; And then add 2 if 740 is even. This selects the left or right side of the metatile
        lda $0740
        and #$01
        eor #$01
        asl a
        adc $02
        ; Use as the offset from the pointer to read the actual tile from the metatile
        tay
        ldx $00
        lda ($0e),y
        sta $03a7,x
        iny
        lda ($0e),y
        sta $03a8,x
        ; $03 is the attribute in the format xx000000
        ; $04 is set to 0 before entering
        ldy $04
        ; $05 is set by the caller
        ; Branch if we are loading the attributes for the right side
        lda $05
        bne @LoadRightAttribute
@LoadLeftAttribute:
        ; Every other attribute (when the loop counter $01 is odd)
        ; moves to the next byte in the attribute buffer
        lda $01
        lsr a
        bcs @BottomLeftAttribute
@TopLeftAttribute:
        rol $03
        rol $03
        rol $03
        jmp @WriteAttribute
@LoadRightAttribute:
        ; Every other attribute (when the loop counter $01 is odd)
        ; moves to the next byte in the attribute buffer
        lda $01
        lsr a
        bcs @BottomRightAttribute
@TopRightAttribute:
        lsr $03
        lsr $03
        lsr $03
        lsr $03
        jmp @WriteAttribute
@BottomLeftAttribute:
        lsr $03
        lsr $03
        ; fallthrough to move to the next attribute byte in the buffer
        ; The bottom right attribute doesn't need shifting
@BottomRightAttribute:
        inc $04
        ; Write attribute data stored in $03 to the attribute buffer
@WriteAttribute:
        lda $0471,y
        ora $03
        sta $0471,y
        inc $00
        inc $00
        ldx $01
        inx
        cpx #$0d
        bcc @Loop
    ; switch back to 0 just in case
    jmp SwapToPRG0

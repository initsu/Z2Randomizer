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
    ; $00 ends up 26 higher (13 rows * 2 bytes), same as before.
    ; X is the buffer offset of the current even row and moves down by 4 per row pair.
    ; Rows are done in pairs (odd row 2k+1, then even row 2k) counting down, starting
    ; with row 12 on its own. $01 holds the even row number 2k.
    lda zp_00
    clc
    adc #26
    sta zp_00
    sec
    sbc #2
    tax                     ; X = offset of row 12
    lda #12
    sta zp_01
    jmp @EvenRow            ; $04 is 0 on entry, so row 12 has no bottom half
@PairLoop:
    ; ---- odd row 2k+1 (bottom attribute quadrant) ----
    ldy zp_01
    lda BackgroundColumnUpdateArray + 1,y
    and #$c0
    lsr a
    lsr a                   ; 00ab0000 bottom-left attribute bits
    sta zp_04
    lsr a
    lsr a
    lsr a                   ; 00000ab0 pointer index
    tay
    lda $8500,y
    sta zp_0e
    lda $8501,y
    sta zp_0f
    ldy zp_01
    lda BackgroundColumnUpdateArray + 1,y
    asl a
    asl a                   ; (id & $3f) * 4, the top two bits fall off
    ora zp_02
    tay
    lda (zp_0e),y
    sta PpuBuffer3Data + 5,x
    iny
    lda (zp_0e),y
    sta PpuBuffer3Data + 6,x
@EvenRow:
    ; ---- even row 2k (top attribute quadrant) ----
    ldy zp_01
    lda BackgroundColumnUpdateArray,y
    and #$c0
    asl a
    rol a
    rol a                   ; 000000ab top-left attribute bits
    sta zp_03
    asl a                   ; 00000ab0 pointer index
    tay
    lda $8500,y
    sta zp_0e
    lda $8501,y
    sta zp_0f
    ldy zp_01
    lda BackgroundColumnUpdateArray,y
    asl a
    asl a
    ora zp_02
    tay
    lda (zp_0e),y
    sta PpuBuffer3Data + 3,x
    iny
    lda (zp_0e),y
    sta PpuBuffer3Data + 4,x
    ; ---- merge both rows into one attribute byte (index k = row / 2) ----
    lda zp_01
    lsr a
    tay
    lda zp_05
    beq @LeftAttr
    ; right side: the left-side bits moved up by one quadrant (<< 2)
    lda zp_04
    ora zp_03
    asl a
    asl a
    jmp @WriteAttr
@LeftAttr:
    lda zp_04
    ora zp_03
@WriteAttr:
    ora $0471,y
    sta $0471,y
    dex
    dex
    dex
    dex
    dec zp_01
    dec zp_01
    bpl @PairLoop
    ; switch back to 0 just in case
    jmp SwapToPRG0

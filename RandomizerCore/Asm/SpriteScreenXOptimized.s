; Contract (identical to vanilla): given an object slot in X and an output
; slot in Y, compute and store
;   * the on-screen slice / visibility mask byte -> $C8,Y
;   * the screen-space X                         -> $CC,Y
;
; Vanilla semantics, preserved exactly:
;   * Four 8px metasprite slices at offsets LF274 = $18,$10,$08,$00 (t=3..0).
;   * Sprite slice is "on screen" (mask bit cleared) iff the HIGH byte of
;       (objectX + 8*i - scrollX)  (16-bit, mod 2^16)  is zero.
;
;   * Visibility bits $F0 are set unless
;       ((objectY + 16) carry) + fall/invisible-state (zp_19/$19) == 1.
;
.include "z2r.inc"

.segment "PRG7"
bank7_BuildSpriteScreenX_And_OffscreenMask := $f27d
.org bank7_BuildSpriteScreenX_And_OffscreenMask
    jmp BuildSpriteScreenX
FREE_UNTIL $f2d3

XOffscreenBitMasks := $f26c      ; $80,$40,$20,$10
XOffscreenBitMasksLow4 := $f270  ; $08,$04,$02,$01
ColumnXOffsetTable := $f274      ; $00,$08,$10,$18,
                                 ; $20,$28,$30,$38,$40

; Link[XY]Position(Hi) is the index 0 entry in the position array
; The arrays also include all enemies and projectiles (see ram.inc).

.reloc
BuildSpriteScreenX:
    sty zp_00
    lda #$00
    ; we accumulate the mask in zp_01 instead of doing STY/LDY juggle into $C8,y
    sta zp_01                         ; slice/visibility mask accumulator
    ; compute `screenX (16-bit) = objectX - scrollX` outside loop
    lda LinkXPosition,x
    sec
    sbc ScrollLeftX
    sta zp_0e                         ; screenX low
    lda LinkXPositionHi,x
    sbc ScrollLeftPage
    sta zp_0f                         ; screenX high
    ldy #$03                          ; four slices
@EdgeLoop:
        lda zp_0e
        clc
        adc ColumnXOffsetTable,y
        lda zp_0f
        adc #$00                          ; high byte of (screenX + offset)
        beq @Aligned                      ; zero -> slice on screen, bit stays clear
@Misaligned:
        lda XOffscreenBitMasksLow4,y
        ora zp_01
        sta zp_01
@Aligned:
        dey
        bpl @EdgeLoop
    ldy zp_00                         ; restores Y from before loop
    lda LinkYPos,x
    clc
    adc #$10
    lda LinkYPositionHi,x
    adc #$00
    cmp #$01
    beq @Store
    lda #$f0
    ora zp_01
    sta zp_01
@Store:
    lda zp_01
    sta LinkOffscreenBits,y
    lda zp_0e
    sta LinkXScreenPosition,y
    rts

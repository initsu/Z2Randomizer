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

.reloc
BuildSpriteScreenX:
    lda LinkXPosition,x
    sec
    sbc ScrollLeftX
    sta LinkXScreenPosition,y     ; final output, also reused below
    lda LinkXPositionHi,x
    sbc ScrollLeftPage            ; A = screenX high
    bne @HiNonZero
    lda LinkXScreenPosition,y     ; hi == 0: only the right edge can clip
    cmp #$e8
    bcs @RightEdge
    lda #$00                      ; all four slices on screen
@StoreMask:
    sta LinkOffscreenBits,y
    lda LinkYPos,x
    cmp #$f0                      ; C = carry out of (Y + $10)
    lda LinkYPositionHi,x
    adc #$ff                      ; zero <=> hi + carry == 1
    bne @YOff
    rts
@YOff:
    lda LinkOffscreenBits,y
    ora #$f0
    sta LinkOffscreenBits,y
    rts

@RightEdge:                       ; hi == 0, lo >= $E8
    cmp #$f0
    bcc @M1
    cmp #$f8
    bcc @M3
    lda #$07
    bne @StoreMask
@M3: lda #$03
    bne @StoreMask
@M1: lda #$01
    bne @StoreMask

@HiNonZero:
    cmp #$ff
    bne @AllOff
    lda LinkXScreenPosition,y     ; hi == $FF: only the left edge slices are on
    cmp #$e8
    bcc @AllOff
    cmp #$f0
    bcc @M0E
    cmp #$f8
    bcc @M0C
    lda #$08
    bne @StoreMask
@M0C: lda #$0c
    bne @StoreMask
@M0E: lda #$0e
    bne @StoreMask
@AllOff:
    lda #$0f
    bne @StoreMask

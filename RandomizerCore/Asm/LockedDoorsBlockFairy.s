; In the US version, collision checks odd/even enemy slots on alternating frames.
; Doors still update often enough to stop Link's velocity and push him back.
; Fairy explicitly skips all recoil ($D9C3), letting it keep moving through.
; (This looks like an accidental interaction, rather than intentional design.)

.include "z2r.inc"

ENEMY_LOCKED_DOOR = $02

.segment "PRG7"
bank7_Update_Enemy_Collision_State := $de63
bank7_Check_Player_Projectile_Hits_Enemy := $e48b
bank7_Update_Enemy_Contact_With_Link := $e4d9

.org bank7_Update_Enemy_Collision_State
    jmp DoorCollisionCadence  ; was jsr bank7_Check_Player_Projectile_Hits_Enemy

.reloc
DoorCollisionCadence:
    lda Enemy0Type,x
    cmp #ENEMY_LOCKED_DOOR
    bne @NotDoor
    ; Match bank7_Update_Enemy_Contact_With_Link logic:
    ; Check collision on odd slot+even frame / even slot+odd frame.
    ; *Except* do not clear EnemyState contact bit for locked doors on the other frame.
    txa
    clc
    adc FrameCounter
    lsr
    bcc @SkipCollision
@NotDoor:
    jsr bank7_Check_Player_Projectile_Hits_Enemy
    jmp $de66                   ; Sword hitbox, then normal player collision.
@SkipCollision:
    rts

# B concept sources

High-resolution concepts that `Tools/build_e_character_b.py` cuts into the eight game
sheets in `Assets/Sprites/E_Character/`. The user chose this B direction on 2026-10-05.

| File | Becomes |
| --- | --- |
| `IdleFacing_B_fourDirections.png` | Idle (one pose per direction) and Master; also the base of every Walk frame |
| `Attack_B_fourDirections.png` | Attack Left / Right / Up |
| `Attack_B_DownFront.png` | Attack Down (front-facing redo) |
| `Hit_B_fourDirections.png` | Hit, 4 directions |
| `Death_B_Down.png` | Death |
| `Skill_B_Down.png` | Skill |
| `Equipment_B_Down.png` | Equipment stills |

Walk is no longer cut from a concept: the generator composes it from the Idle pose
(see `Docs/E_SPRITE_WORKLOG.md`). The B walk concepts, the A/B comparison concepts and
the colour experiments were deleted in the 2026-10-06 cleanup.

Generated with the built-in image generation tool, using the A `E_Master.png` and the A
`E_Attack.png` as visual and timing references. The concepts drift in armour detail and
weapon hand between frames; the generator corrects size, position, colour and stray
pixels, but not drawing differences.

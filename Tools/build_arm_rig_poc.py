"""Arm rig proof of concept: armless bodies (4 directions) + arm pieces + pivots.

Run after `build_e_character_b.py`:  python -B Tools/build_arm_rig_poc.py
Writes Assets/Resources/ArmRig/ for the ARM RIG buttons of the EAnimationPreview
scene ("Tools > E Character > Build Animation Preview"). Requires Pillow, numpy
and scipy. Plan: Docs/ARM_RIG_PLAN.md.

The arm pieces are cut once, from the screen-right arm of Idle Down, and shared by
every direction (mirrored for the other side) so the arms cannot drift apart
between directions. Per direction only three things differ: where the shoulders
are, whether each arm is drawn in front of or behind the body, and which pixels of
the Idle drawing are arm/weapon and must be erased. Coordinates are pixels of the
64px Idle cell (x right, y down).
"""

import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
import numpy as np
from PIL import Image

from build_e_character_b import body_bottom
from e_color_transfer import band_of, oklab

ROOT = Path(__file__).resolve().parents[1]
IDLE = ROOT / "Assets/Sprites/E_Character/E_Idle.png"
OUT = ROOT / "Assets/Resources/ArmRig"
CELL = 64
ROWS = ("Down", "Left", "Right", "Up")

# --- arm pieces (from Idle Down, screen-right arm) ---
ARM_SOURCE = (                     # (x0, y0, x1, y1) rectangles of that arm, non-cape pixels
    (46, 23, 63, 38), (48, 39, 63, 49))
SHOULDER = (48, 26)     # shoulder guard gem: upper arm pivot
ELBOW = (49, 37)        # forearm pivot
GRIP = (51, 46)         # fist centre: where a weapon handle goes
ELBOW_SPLIT_ROW = 37    # first forearm row (old one-piece split, kept for comparison)
PAULDRON_LAST_ROW = 29  # shoulder guard: stays on the torso, the arm turns under it
BARE_UPPER_ROWS = (28, 38)   # upper arm without the guard; tucks 2 rows under it
JOINT_FORE_FIRST = 35        # forearm overlaps the upper arm 3 rows so a bent elbow shows no gap

# --- per direction ---
# main = weapon arm, off = other arm: (x, y, side, front). side +1 = piece as cut
# (a screen-right arm), -1 = mirrored. front = drawn over the body. erase =
# rectangles whose non-cape pixels are arm or weapon in that Idle drawing; a fifth
# value True erases every pixel in it (no cape there, but the sword hilt and glove
# have red pixels that would otherwise be kept as "cape").
# hip = last torso row (legs below); None = measure it.
DIRECTIONS = {
    "Down": dict(main=(48, 26, 1, True), off=(15, 26, -1, True),
                 erase=((46, 23, 63, 38), (48, 39, 63, 49),          # screen-right arm
                        (0, 23, 17, 38), (0, 39, 16, 52)),           # screen-left arm + sword
                 hip=None),
    # The near arm hangs in front of the cape; the sword arm is hidden behind the
    # torso with only the fist and sword showing ahead of it.
    "Left": dict(main=(37, 27, 1, True), off=(27, 28, -1, False),
                 erase=((33, 23, 44, 49),                            # near arm
                        (0, 30, 24, 50, True)),                      # sword + fist
                 hip=50),
    # Not a mirror of Left: here the sword arm reaches out in front of the torso.
    "Right": dict(main=(36, 28, 1, True), off=(26, 27, -1, False),
                  erase=((35, 24, 56, 48, True),                     # sword arm + sword
                         (17, 25, 29, 49)),                          # back arm
                  hip=50),
    # From behind both arms hang outside the cape; swinging forward (up the screen)
    # takes them behind the body, so both are drawn behind it.
    "Up": dict(main=(49, 30, 1, False), off=(14, 30, -1, False),
               erase=((46, 27, 63, 48),                              # screen-right arm + sword
                      (0, 27, 15, 50)),                              # screen-left arm
               hip=None),
}


def cape_mask(tile):
    return band_of(oklab(tile[..., :3])) == 1


def rect_mask(tile, rects):
    visible = tile[..., 3] > 0
    not_cape = visible & ~cape_mask(tile)
    mask = np.zeros(visible.shape, bool)
    for x0, y0, x1, y1, *every in rects:
        region = np.zeros(visible.shape, bool)
        region[y0:y1 + 1, x0:x1 + 1] = True
        mask |= region & (visible if every and every[0] else not_cape)
    return mask


def fill_cape(body, hole, cape):
    """Arms hung in front of the cape: grow cape colour into the hole, but only
    where the cape continues past it (otherwise the hole was empty space)."""
    cape = cape & (body[..., 3] > 0)
    pending = hole.copy()
    for _ in range(16):
        grown = False
        for y, x in zip(*np.nonzero(pending)):
            outward = range(x + 1, CELL) if x > CELL // 2 else range(x - 1, -1, -1)
            if not (any(cape[y, k] for k in outward) or cape[y + 1:, x].any()):
                continue
            for dy, dx in ((-1, 0), (0, 1), (0, -1), (1, 0)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < CELL and 0 <= nx < CELL and cape[ny, nx]:
                    body[y, x] = body[ny, nx]
                    cape[y, x] = True
                    pending[y, x] = False
                    grown = True
                    break
        if not grown:
            break


def piece(arm, rows, pivot):
    part = np.zeros_like(arm)
    part[rows] = arm[rows]
    image = Image.fromarray(part)
    left, top, right, bottom = image.getbbox()
    return image.crop((left, top, right, bottom)), (pivot[0] - left, pivot[1] - top)


ICONS = ROOT / "Assets/2D Medieval Weapons icon pack by Podmeteya/PNG/200x448"
# Test weapons for the three base motions: (output name, icon, length px, motion).
# Placeholders only; real weapon art is decided per weapon with the user.
TEST_WEAPONS = (("Sword", "Sword-1", 24, "Slash"),
                ("Spear", "Staff-1", 34, "Thrust"),
                ("Club", "Bat-1", 22, "Smash"))
GRIP_FROM_BOTTOM = 0.10   # handle point held in the fist, as a share of length


def export_weapons():
    weapons = []
    for name, icon, length, motion in TEST_WEAPONS:
        with Image.open(ICONS / f"{icon}-Transparent.png") as source:
            image = source.convert("RGBA")
        image = image.crop(image.getbbox())
        width = max(1, round(image.width * length / image.height))
        small = image.resize((width, length), Image.Resampling.LANCZOS)
        pixels = np.array(small)
        pixels[..., 3] = np.where(pixels[..., 3] >= 128, 255, 0)
        Image.fromarray(pixels).save(OUT / f"Weapon_{name}.png")
        grip = (width // 2, length - 1 - round(length * GRIP_FROM_BOTTOM))
        weapons.append({"name": name, "motion": motion, "grip": grip})
    return weapons


def export_arm_pieces(down):
    arm = np.zeros_like(down)
    mask = rect_mask(down, ARM_SOURCE)
    arm[mask] = down[mask]
    pieces = {
        "E_Arm_Upper": piece(arm, slice(0, ELBOW_SPLIT_ROW + 1), SHOULDER),
        "E_Arm_Fore": piece(arm, slice(ELBOW_SPLIT_ROW, CELL), ELBOW),
        "E_Pauldron": piece(arm, slice(0, PAULDRON_LAST_ROW + 1), SHOULDER),
        "E_Arm_UpperBare": piece(arm, slice(BARE_UPPER_ROWS[0], BARE_UPPER_ROWS[1] + 1), SHOULDER),
        "E_Arm_ForeJoint": piece(arm, slice(JOINT_FORE_FIRST, CELL), ELBOW),
    }
    for name, (image, _) in pieces.items():
        image.save(OUT / f"{name}.png")
    return {name: pivot for name, (_, pivot) in pieces.items()}


def export_body(direction, tile):
    """Armless upper body (torso, cape) and planted legs on the same canvas: an
    attack can crouch or lean the upper body while the feet stay on the ground."""
    config = DIRECTIONS[direction]
    cape = cape_mask(tile)
    hole = rect_mask(tile, config["erase"])
    body = tile.copy()
    body[hole] = 0
    fill_cape(body, hole, cape)
    hip = config["hip"] if config["hip"] is not None else body_bottom(tile[..., 3] > 0)
    leg_mask = (body[..., 3] > 0) & ~cape_mask(body)
    leg_mask[:hip + 1] = False
    legs = np.zeros_like(body)
    legs[leg_mask] = body[leg_mask]
    upper = body.copy()
    upper[leg_mask] = 0
    Image.fromarray(upper).save(OUT / f"E_Body_{direction}_Upper.png")
    Image.fromarray(legs).save(OUT / f"E_Body_{direction}_Legs.png")
    main, off = config["main"], config["off"]
    return {"name": direction,
            "mainShoulder": main[:2], "mainSide": main[2], "mainFront": main[3],
            "offShoulder": off[:2], "offSide": off[2], "offFront": off[3]}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    with Image.open(IDLE) as sheet:
        idle = np.array(sheet.convert("RGBA"))
    tiles = {name: idle[row * CELL:(row + 1) * CELL, :CELL] for row, name in enumerate(ROWS)}
    pivots = export_arm_pieces(tiles["Down"])
    rig = {
        "cell": CELL,
        "elbowFromShoulder": (ELBOW[0] - SHOULDER[0], ELBOW[1] - SHOULDER[1]),
        "gripFromElbow": (GRIP[0] - ELBOW[0], GRIP[1] - ELBOW[1]),
        "upperPivot": pivots["E_Arm_Upper"],
        "forePivot": pivots["E_Arm_Fore"],
        "pauldronPivot": pivots["E_Pauldron"],
        "bareUpperPivot": pivots["E_Arm_UpperBare"],
        "jointForePivot": pivots["E_Arm_ForeJoint"],
        "directions": [export_body(name, tiles[name]) for name in ROWS],
        "weapons": export_weapons(),
    }
    (OUT / "rig.json").write_text(json.dumps(rig, indent=2), encoding="utf-8")
    print("arm rig:", json.dumps(rig["directions"]))


if __name__ == "__main__":
    main()

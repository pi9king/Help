"""Draft key poses for the arm rig's base motions -> Assets/Resources/ArmRig/arm_motions.json.

Run: python -B Tools/arm_motion_defaults.py   (overwrites the JSON — run only to reset to these drafts)
The EAnimationPreview key panel edits and saves the same JSON, so after tuning in Play this
script is NOT the source of truth anymore. Plan: Docs/ARM_RIG_PLAN.md.

Each pose: hand = (x, y) px from the weapon shoulder on screen (y up), weapon = screen angle in
degrees (0 = right, 90 = up; sweeps follow the numbers as written, not the shortest way),
front = draw the weapon arm in front of the body, shoulder = (x, y) px the shoulder itself moves
(the hand is relative to it, so it moves too), elbowFlip = use the other IK solution.
All values are model drafts to be tuned.
"""

import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "Assets/Resources/ArmRig/arm_motions.json"


def pose(name, x, y, weapon, front=True, shoulder=(0, 0), flip=False):
    return {"name": name, "hand": [x, y], "weapon": weapon, "front": front,
            "shoulder": list(shoulder), "elbowFlip": flip}


def mirror(keys):
    """Right-facing poses -> Left-facing: flip x and mirror the weapon angle."""
    return [pose(k["name"], -k["hand"][0], k["hand"][1], 180 - k["weapon"], k["front"],
                 (-k["shoulder"][0], k["shoulder"][1]), k["elbowFlip"]) for k in keys]


# Rest = the Idle drawing itself. The hand sits exactly where the cut arm pieces put it (elbow (1,11) +
# fist (2,9) from the shoulder), so IK solves to zero rotation and the arm looks like the Idle sprite;
# the weapon angle is the sword in each original Idle frame (Down is mirrored: the rig holds it in the
# screen-right hand). User: "애니메이션 끝의 팔을 Idle 상태로". Up is drawn behind the body.
RESTS = [
    {"direction": "Down", "hand": [3, -20], "weapon": 51, "front": True},
    {"direction": "Left", "hand": [3, -20], "weapon": 143, "front": True},
    {"direction": "Right", "hand": [3, -20], "weapon": 37, "front": True},
    {"direction": "Up", "hand": [3, -20], "weapon": 40, "front": False},
]

# --- Right (facing right). Slash: a flat sweep from behind the body to the front, not a circle.
# Wound up high behind the shoulder and kept in front of the body: drawn behind it the arm
# vanished for the whole windup (user: "팔이 사라지는거 거슬리는데").
RIGHT = {
    "Slash": ("Smooth", [pose("Loaded", -6, 7, 150), pose("Cocked", -8, 8, 160),
                         pose("Mid", 15, -6, 5), pose("Through", 11, -12, -40)]),
    # Thrust: hand back at the hip, tip on the aim the whole time, then all the way out.
    "Thrust": ("Snap", [pose("Loaded", -4, -9, 0), pose("Cocked", -7, -9, 0),
                        pose("Mid", 12, -7, 0), pose("Through", 19, -6, 0)]),
    # Smash: raised high and back over the head, comes down in front.
    "Smash": ("Accelerate", [pose("Loaded", -2, 12, 110), pose("Cocked", -4, 13, 125),
                             pose("Mid", 13, 5, 30), pose("Through", 12, -13, -75)]),
}

TRACKS = {
    "Down": {
        # Facing the camera, weapon arm on screen right: sweep across the belly to the other side.
        "Slash": ("Smooth", [pose("Loaded", 13, -7, 20), pose("Cocked", 14, -5, 35),
                             pose("Mid", 0, -17, -90), pose("Through", -16, -11, -165)]),
        # Toward the camera: the tip points down the whole time. The shoulder pulls back (up) on the
        # chamber and drives toward the aim (down) on the hit; the elbow lifts out instead of folding
        # in (user: "팔꿈치가 반대로 꺾이는 느낌", "어깨까지 사용해서 좀 더 찌른다는 느낌").
        "Thrust": ("Snap", [pose("Loaded", 8, -8, -90, shoulder=(1, 2), flip=True),
                            pose("Cocked", 9, -6, -90, shoulder=(1, 3), flip=True),
                            pose("Mid", 3, -15, -90, shoulder=(0, -1), flip=True),
                            # Out of reach on purpose: the arm locks fully straight. A nearly straight
                            # arm bent outward read as a hyperextended elbow (user: last frame).
                            pose("Through", 1, -21, -90, shoulder=(-1, -4), flip=True)]),
        # Over the head and down the outside: unlike the thrust the tip sweeps from up to down.
        "Smash": ("Accelerate", [pose("Loaded", 5, 11, 100), pose("Cocked", 3, 13, 115),
                                 pose("Mid", 13, -1, 10), pose("Through", 5, -18, -95)]),
    },
    "Right": RIGHT,
    "Left": {kind: (curve, mirror(keys)) for kind, (curve, keys) in RIGHT.items()},
    "Up": {
        # Seen from behind: the arm is drawn behind the body (user: "Up이면 카메라 기준 뒤에서 보이니까,
        # 팔이 안 보여야겠지"), only the blade shows above the head.
        # Forward is up the screen here, so the attacks must travel up or they read like Down
        # (user: "Up은 더 이상해졌어 ... 애니메이션은 Down과 똑같아 진 것 같은데").
        # Slash: blade rests back over the shoulder (pointing down the screen), arcs over the head
        # and finishes forward-left.
        "Slash": ("Smooth", [pose("Loaded", 11, 4, -40, False), pose("Cocked", 12, 6, -55, False),
                             pose("Mid", 3, 13, 95, False), pose("Through", -11, 8, 165, False)]),
        # Thrust: from low at the right side straight up; the hand never crosses the back.
        "Thrust": ("Snap", [pose("Loaded", 8, -10, 90, False, shoulder=(0, -1)), pose("Cocked", 8, -12, 90, False, shoulder=(0, -2)),
                            pose("Mid", 7, 6, 90, False, shoulder=(0, 1)), pose("Through", 6, 21, 90, False, shoulder=(0, 3))]),
        # Smash: thrown back over the shoulder, then brought over the head hard and high.
        "Smash": ("Accelerate", [pose("Loaded", 7, 9, -70, False), pose("Cocked", 6, 11, -85, False),
                                 pose("Mid", 9, 13, 30, False), pose("Through", 5, 17, 95, False)]),
    },
}


def main():
    tracks = [{"motion": kind, "direction": direction, "curve": curve, "keys": keys}
              for direction, motions in TRACKS.items() for kind, (curve, keys) in motions.items()]
    OUT.write_text(json.dumps({"rests": RESTS, "tracks": tracks}, indent=2), encoding="utf-8")
    print(f"wrote {OUT.name}: {len(tracks)} tracks")


if __name__ == "__main__":
    main()

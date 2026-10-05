"""Build the chosen Concept B sheets and all derived Unity/preview sprites.

Run `python -B Tools/build_e_character_b.py` from the project root.
Requires Pillow, numpy and scipy.
The colour reference (the original A master) is ArtSource/E_Animation/Archive/BeforeB/E_Master.png.
"""

import sys
from pathlib import Path

sys.dont_write_bytecode = True
from PIL import Image
import numpy as np
from scipy import ndimage
from e_color_transfer import Reference, band_of, oklab, recolor
from e_sprite_cleanup import cells, despill_sheet

ROOT = Path(__file__).resolve().parents[1]
CONCEPTS = ROOT / "ArtSource/E_Animation/Concepts"
ART = ROOT / "ArtSource/E_Animation"
UNITY = ROOT / "Assets/Sprites/E_Character"
MAGENTA = ART / "64px_Magenta"
PREVIEW = ART / "Preview"
FRAMES = ART / "Frames_Transparent"
FRAMES_MAGENTA = ART / "Frames_Magenta"
MASTER_DERIVED = ROOT / "ArtSource/E_Master/64px"
CELL = 64
ROW_NAMES = ("Down", "Left", "Right", "Up")

A_MASTER = ART / "Archive/BeforeB/E_Master.png"


def restore_a_colors(path, reference):
    with Image.open(path) as source:
        image = despill_sheet(source)
    recolor(image, reference).save(path)


def magenta(image):
    background = Image.new("RGBA", image.size, (255, 0, 255, 255))
    background.alpha_composite(image)
    return background.convert("RGB")


def write(name, grid):
    cols, rows = grid
    path = UNITY / f"E_{name}.png"
    with Image.open(path) as image:
        image = image.convert("RGBA")
        assert image.size == (cols * CELL, rows * CELL), (name, image.size)
        colored = magenta(image)
        colored.save(MAGENTA / f"E_{name}.png")
        colored.resize((colored.width * 4, colored.height * 4),
                       Image.Resampling.NEAREST).save(PREVIEW / f"E_{name}_x4.png")
        for row in range(rows):
            direction = ROW_NAMES[row] if rows == 4 else "Down"
            folder = FRAMES / (f"{name}_{direction}" if rows == 4 else name)
            magenta_folder = FRAMES_MAGENTA / (f"{name}_{direction}" if rows == 4 else name)
            folder.mkdir(parents=True, exist_ok=True)
            magenta_folder.mkdir(parents=True, exist_ok=True)
            for col in range(cols):
                frame = image.crop((col * CELL, row * CELL, (col + 1) * CELL, (row + 1) * CELL))
                if frame.getbbox() is None:
                    raise ValueError(f"Empty frame: {name} {row} {col}")
                frame.save(folder / f"Frame_{col + 1:02}.png")
                magenta(frame).save(magenta_folder / f"Frame_{col + 1:02}.png")
    print(f"E_{name}: {cols}x{rows}")


BODY_HEIGHT = 52  # Idle height; every state keeps the same E size.
FOOT_BAND = 7
HEAD_BAND = 6


def feet_center(image):
    alpha = image.getchannel("A")
    feet = [x for y in range(max(0, image.height - FOOT_BAND), image.height)
            for x in range(image.width) if alpha.getpixel((x, y))]
    return sum(feet) / len(feet) if feet else None


def head_center(image):
    alpha = image.getchannel("A")
    head = [x for y in range(HEAD_BAND) for x in range(image.width) if alpha.getpixel((x, y))]
    return (min(head) + max(head)) / 2


def body_metrics(alpha):
    """(top bar width, top row, torso bottom row) of a placed 64px tile."""
    top = int(np.flatnonzero(alpha.any(axis=1))[0])
    head = np.flatnonzero(alpha[top:top + HEAD_BAND].any(axis=0))
    return int(head[-1] - head[0] + 1), top, body_bottom(alpha)


def resize_frame(frame, scale, fx=1.0, torso_y=1.0, legs_y=1.0, legs_source=0):
    """Scale a concept frame to sprite size. legs_source (concept px, measured up
    from the feet) splits torso from legs so each gets its own vertical factor."""
    width = max(1, round(frame.width * scale * fx))
    if legs_source <= 0 or (torso_y == 1.0 and legs_y == 1.0):
        return frame.resize((width, max(1, round(frame.height * scale))), Image.Resampling.NEAREST)
    split = max(1, min(frame.height - 1, round(frame.height - legs_source)))
    upper = frame.crop((0, 0, frame.width, split))
    lower = frame.crop((0, split, frame.width, frame.height))
    upper = upper.resize((width, max(1, round(upper.height * scale * torso_y))), Image.Resampling.NEAREST)
    lower = lower.resize((width, max(1, round(lower.height * scale * legs_y))), Image.Resampling.NEAREST)
    joined = Image.new("RGBA", (width, upper.height + lower.height))
    joined.paste(upper, (0, 0))
    joined.paste(lower, (0, upper.height))
    return joined


def place_sheet(path, rows, heads=None, idle=None):
    """Scale each row so its first (neutral) pose is BODY_HEIGHT tall, then put
    the grounded feet on the pivot column. Width is never used to shrink a row:
    wide capes and swords are clipped at the cell edge instead, so switching
    between Idle, Walk and Attack does not change the character's size.

    heads: the Idle E top-bar center per row. Every state lines its body up with
    Idle so changing state does not jump the body sideways (it jumped 4-7px):
    frames keep their foot-based spacing (drawn lunges and crouches survive) and
    the whole row shifts so frame 0 meets the Idle head."""
    columns = len(rows[0])
    sheet = Image.new("RGBA", (columns * CELL, len(rows) * CELL))
    for row, frames in enumerate(rows):
        scale = min(BODY_HEIGHT / frames[0].height,
                    60 / max(frame.height for frame in frames))
        resized = [resize_frame(frame, scale) for frame in frames]
        if idle is not None:
            # Match the E's proportions to Idle (user decision, 2026-10-05): every
            # state was generated with its own build, e.g. Walk Down had a shorter
            # torso and longer legs, so Idle -> Walk looked squashed. Factors come
            # from frame 0 and apply to the whole row, so drawn crouches survive.
            first = np.zeros((61, resized[0].width), bool)
            first[61 - resized[0].height:] = np.array(resized[0].getchannel("A")) > 0
            head_width, top, bottom = body_metrics(first)
            idle_head, idle_top, idle_bottom = idle[row]
            legs_source = (60 - bottom) / scale
            factors = dict(fx=idle_head / head_width)
            # The torso/leg split is only measurable from the front and back: from
            # the side the torso is as narrow as an arm and the measurement lands
            # on the sword, which stretched the boots.
            if len(rows) == 1 or ROW_NAMES[row] in ("Down", "Up"):
                factors.update(torso_y=(idle_bottom - idle_top + 1) / (bottom - top + 1),
                               legs_y=(60 - idle_bottom) / (60 - bottom),
                               legs_source=legs_source)
            resized = [resize_frame(frame, scale, **factors) for frame in frames]
        lefts = []
        for column, image in enumerate(resized):
            feet = feet_center(image)
            if feet is None:
                raise ValueError(f"No grounded foot pixels: {path} row={row} col={column}")
            lefts.append(CELL / 2 - feet)
        if heads is not None:
            shift = heads[row] - (lefts[0] + head_center(resized[0]))
            lefts = [left + shift for left in lefts]
        for column, (image, left) in enumerate(zip(resized, lefts)):
            tile = Image.new("RGBA", (CELL, CELL))
            # paste() clips at the tile edge; alpha_composite() rejects negative offsets.
            tile.paste(image, (round(left), 61 - image.height), image)
            # Keep a clear one-pixel gutter so clipped tips never touch a neighbor.
            for y in range(CELL):
                tile.putpixel((0, y), (0, 0, 0, 0))
                tile.putpixel((CELL - 1, y), (0, 0, 0, 0))
            sheet.alpha_composite(tile, (column * CELL, row * CELL))
    sheet.save(path)


def idle_proportions(path):
    with Image.open(path) as idle:
        alpha = np.array(idle.convert("RGBA"))[..., 3] > 0
    return [body_metrics(alpha[row * CELL:(row + 1) * CELL, :CELL])
            for row in range(alpha.shape[0] // CELL)]


def idle_heads(path):
    with Image.open(path) as idle:
        idle = idle.convert("RGBA")
        heads = []
        for row in range(idle.height // CELL):
            tile = idle.crop((0, row * CELL, CELL, (row + 1) * CELL))
            top = tile.getbbox()[1]
            heads.append(head_center(tile.crop((0, top, CELL, CELL))))
        return heads


def body_bottom(alpha):
    """Lowest row of the E torso: the last row where the central span is still
    wider than a pair of legs."""
    for y in range(56, 29, -1):
        row = alpha[y, 14:50]
        run = best = 0
        for filled in row:
            run = run + 1 if filled else 0
            best = max(best, run)
        if best >= 20:
            return y
    raise ValueError("No torso found")


def foot_spans(alpha, bottom):
    """Left and right boot (start, end) columns below the torso. Thin runs are
    cape or blade tips, not boots."""
    columns = np.flatnonzero(alpha[bottom + 1:61].any(axis=0))
    runs = np.split(columns, np.flatnonzero(np.diff(columns) > 1) + 1)
    boots = sorted((run for run in runs if run.size >= 8), key=lambda run: run[0])
    if len(boots) < 2:
        raise ValueError("Expected two boots under the torso")
    return (boots[0][0], boots[0][-1]), (boots[-1][0], boots[-1][-1])


def fit_attack_down_legs():
    """Attack Down was generated with wider boots and a wider stance than Idle,
    and its boot size also drifts between frames (15px in frame 1, up to 22px at
    impact), so the feet grew the moment an attack started and kept growing.

    Each boot of each frame is resized to the Idle boot width. Where the boots
    stand still follows the attack drawing, scaled so frame 1 has Idle's stance,
    so the legs still step with the torso (pasting static Idle legs left the body
    moving on frozen legs). Cape and blade pixels in front of the legs are kept."""
    with Image.open(UNITY / "E_Idle.png") as idle_sheet:
        idle = np.array(idle_sheet.convert("RGBA"))[:CELL, :CELL]
    idle_alpha = idle[..., 3] > 0
    idle_boots = foot_spans(idle_alpha, body_bottom(idle_alpha))
    idle_widths = [end - start + 1 for start, end in idle_boots]
    idle_centers = [(start + end) / 2 for start, end in idle_boots]

    path = UNITY / "E_Attack.png"
    with Image.open(path) as attack_sheet:
        sheet = np.array(attack_sheet.convert("RGBA"))
    first = sheet[:CELL, :CELL, 3] > 0
    first_centers = [(start + end) / 2 for start, end in foot_spans(first, body_bottom(first))]
    stance = (idle_centers[1] - idle_centers[0]) / (first_centers[1] - first_centers[0])
    middle = sum(first_centers) / 2
    idle_middle = sum(idle_centers) / 2

    for column in range(sheet.shape[1] // CELL):
        tile = sheet[:CELL, column * CELL:(column + 1) * CELL]
        visible = tile[..., 3] > 0
        bottom = body_bottom(visible)
        lab = oklab(tile[..., :3])
        band = band_of(lab)
        in_front = visible & ((band == 1) | ((band == 0) & (lab[..., 0] > 0.78)))
        in_front[:bottom + 1] = False
        legs = tile.copy()
        legs[in_front] = 0
        merged = tile.copy()
        merged[bottom + 1:] = 0
        for (start, end), width in zip(foot_spans(visible, bottom), idle_widths):
            center = idle_middle + ((start + end) / 2 - middle) * stance
            left = round(center - (width - 1) / 2)
            for offset in range(width):
                source = start + round(offset * (end - start) / max(1, width - 1))
                if 0 <= left + offset < CELL:
                    column_pixels = legs[bottom + 1:, source]
                    target = merged[bottom + 1:, left + offset]
                    filled = column_pixels[:, 3] > 0
                    target[filled] = column_pixels[filled]
        merged[in_front] = tile[in_front]
        # Cape tips that hung off the old, wider boots now float as specks.
        below = merged[..., 3] > 0
        below[:bottom + 1] = False
        labels, count = ndimage.label(merged[..., 3] > 0, structure=np.ones((3, 3)))
        sizes = ndimage.sum(merged[..., 3] > 0, labels, range(1, count + 1))
        specks = below & np.isin(labels, np.flatnonzero(sizes < 3) + 1)
        merged[specks] = 0
        sheet[:CELL, column * CELL:(column + 1) * CELL] = merged
    Image.fromarray(sheet).save(path)


BAR_LEFT_CAP = 3   # outline + gold trim kept intact at each end of the bar
BAR_RIGHT_CAP = 4


def bar_run(solid, y, x):
    """Contiguous [start, end] of non-cape pixels through column x on row y."""
    if not solid[y, x]:
        return None
    start = end = x
    while start > 0 and solid[y, start - 1]:
        start -= 1
    while end < CELL - 1 and solid[y, end + 1]:
        end += 1
    return start, end


def match_bottom_bar(tile, center):
    """Stretch the E's bottom bar to the top bar's span (user decision,
    2026-10-05: the bars were 31px and 26px in Idle Down). End caps stay as drawn;
    only the middle is stretched. Returns False when the frame has no clear bars
    (Death collapse, effects)."""
    alpha = tile[..., 3] > 0
    solid = alpha & (band_of(oklab(tile[..., :3])) != 1)
    x = int(round(center))
    top_row = next((y for y in range(CELL) if (run := bar_run(solid, y, x)) and run[1] - run[0] >= 20), None)
    if top_row is None:
        return False
    top_span = bar_run(solid, top_row + 3, x)
    try:
        bottom = body_bottom(alpha)
    except ValueError:
        return False
    span = bar_run(solid, bottom - 4, x)
    if top_span is None or span is None:
        return False
    target = (min(top_span[0], span[0]), max(top_span[1], span[1]))
    grow = (target[1] - target[0]) - (span[1] - span[0])
    if grow <= 0 or grow > 12:
        return False
    first = bottom
    while first > top_row + 10:
        run = bar_run(solid, first - 1, x)
        if run is None or run[1] < span[1] - 2:
            break
        first -= 1
    source = tile.copy()
    inner_from = (span[0] + BAR_LEFT_CAP, span[1] - BAR_RIGHT_CAP)
    inner_to = (target[0] + BAR_LEFT_CAP, target[1] - BAR_RIGHT_CAP)
    for y in range(first, bottom + 1):
        row = source[y]
        for offset in range(BAR_LEFT_CAP):
            tile[y, target[0] + offset] = row[span[0] + offset]
        for offset in range(BAR_RIGHT_CAP + 1):
            tile[y, target[1] - offset] = row[span[1] - offset]
        width_to = inner_to[1] - inner_to[0]
        width_from = inner_from[1] - inner_from[0]
        for k in range(width_to + 1):
            tile[y, inner_to[0] + k] = row[inner_from[0] + round(k * width_from / max(1, width_to))]
    return True


def match_down_bars(heads):
    """Apply match_bottom_bar to every front-facing frame."""
    changed = {}
    for name, rows in (("Idle", (0,)), ("Attack", (0,)), ("Hit", (0,)),
                       ("Skill", (0,)), ("Equipment", (0,)), ("Death", (0,))):
        path = UNITY / f"E_{name}.png"
        with Image.open(path) as image:
            sheet = np.array(image.convert("RGBA"))
        for row in rows:
            for column in range(sheet.shape[1] // CELL):
                tile = sheet[row * CELL:(row + 1) * CELL, column * CELL:(column + 1) * CELL]
                if match_bottom_bar(tile, heads[0]):
                    changed.setdefault(name, []).append(column + 1)
        Image.fromarray(sheet).save(path)
    return changed


# Per Walk frame: (screen-left boot lift, screen-right boot lift, body rise) in px.
# Contact -> lift -> lower for each foot, and the upper body rides 1px higher
# while a foot is off the ground. The concept lifted a different boot every frame
# (three steps per loop); with a body rise added that read as vibrating.
WALK_STEPS = ((0, 0, 0), (0, 4, 1), (0, 2, 1), (0, 0, 0), (4, 0, 1), (2, 0, 1))


def compose_front_walks():
    """Front and back Walk = Idle upper body + Idle boots moved per WALK_STEPS.

    The generated Walk Down redrew the E torso differently in every frame, so the
    bottom bar broke up while walking, and Walk Up lifted the same foot in frames
    4-6 so it stopped stepping (user report, 2026-10-05). Both rows now share one
    torso drawing and one step pattern."""
    with Image.open(UNITY / "E_Idle.png") as idle_sheet:
        idle = np.array(idle_sheet.convert("RGBA"))
    path = UNITY / "E_Walk.png"
    with Image.open(path) as walk_sheet:
        walk = np.array(walk_sheet.convert("RGBA"))
    for row in (ROW_NAMES.index("Down"), ROW_NAMES.index("Up")):
        tile = idle[row * CELL:(row + 1) * CELL, :CELL]
        alpha = tile[..., 3] > 0
        bottom = body_bottom(alpha)
        left_boot, right_boot = foot_spans(alpha, bottom)
        split = (left_boot[1] + right_boot[0]) // 2
        lab = oklab(tile[..., :3])
        band = band_of(lab)
        # Everything non-cape inside the boot columns moves with the boot,
        # including its bright highlights (they stayed behind as specks).
        boot = alpha & (band != 1)
        boot[:bottom + 1] = False
        in_columns = np.zeros(CELL, bool)
        for start, end in (left_boot, right_boot):
            in_columns[max(0, start - 1):end + 2] = True
        boot[:, ~in_columns] = False
        upper = np.zeros_like(tile)
        upper[:bottom + 1] = tile[:bottom + 1]
        loose = tile.copy()           # cape or blade pixels hanging below the torso
        loose[:bottom + 1] = 0
        loose[boot] = 0
        # Drop 1-2px cape crumbs that float under the cape in the Idle drawing.
        labels, count = ndimage.label(loose[..., 3] > 0, structure=np.ones((3, 3)))
        sizes = ndimage.sum(loose[..., 3] > 0, labels, range(1, count + 1))
        loose[np.isin(labels, np.flatnonzero(sizes < 3) + 1)] = 0
        for column, (*lifts, rise) in enumerate(WALK_STEPS):
            frame = np.zeros_like(tile)
            for (start, end), lift in zip(((0, split), (split + 1, CELL - 1)), lifts):
                part = np.zeros_like(tile)
                part[:, start:end + 1][boot[:, start:end + 1]] = tile[:, start:end + 1][boot[:, start:end + 1]]
                if lift:
                    part = np.roll(part, -lift, axis=0)
                    part[-lift:] = 0
                elif rise:
                    # A planted leg reaches up to the raised torso, so no gap opens.
                    part[bottom + 1 - rise:bottom + 1] = part[bottom + 1]
                filled = part[..., 3] > 0
                frame[filled] = part[filled]
            # Cape tips and the sword hanging below the torso ride with it.
            for layer, shift in ((loose, rise), (upper, rise)):
                moved = np.roll(layer, -shift, axis=0) if shift else layer
                filled = moved[..., 3] > 0
                frame[filled] = moved[filled]
            walk[row * CELL:(row + 1) * CELL, column * CELL:(column + 1) * CELL] = frame
    Image.fromarray(walk).save(path)


# Side Walk, per frame, in px toward the facing direction: near and far foot
# offsets, their lifts, and the body rise. Each foot slides back while planted and
# swings forward while lifted; the two legs are half a cycle apart. The rise uses
# the same frames as WALK_STEPS so every direction bobs together.
SIDE_NEAR = ((6, 0), (2, 0), (-2, 0), (-6, 0), (-2, 3), (2, 2))
SIDE_FAR = ((-6, 0), (-2, 3), (2, 2), (6, 0), (2, 0), (-2, 0))
# From the side the torso measurement lands on the arm guard; the legs proper
# (knee gem down) are the last 10 rows above the ground.
SIDE_LEG_ROWS = 10
SIDE_RISE = tuple(step[2] for step in WALK_STEPS)
FAR_LEG_SHADE = 0.72


def swing_leg(leg, hip, offset, lift, rise):
    """Lean a leg around its hip: the foot moves by offset/lift, the hip rises
    with the body, and rows in between are interpolated (a pendulum, not a slide)."""
    out = np.zeros_like(leg)
    rows, cols = np.nonzero(leg[..., 3] > 0)
    span = max(1, 60 - hip)
    for y, x in zip(rows, cols):
        t = (y - hip) / span
        ny = y - round(rise * (1 - t) + lift * t)
        nx = x + round(offset * t)
        if 0 <= ny < CELL and 0 <= nx < CELL:
            out[ny, nx] = leg[y, x]
    return out


def compose_side_walks(reference):
    """Left/Right Walk = Idle side upper body + the Idle legs drawn twice (near and
    a darker far copy) swinging around the hip.

    The generated side walk redrew the sword at a new angle every frame and its
    feet mostly bobbed up and down in place (user report, 2026-10-05)."""
    with Image.open(UNITY / "E_Idle.png") as idle_sheet:
        idle = np.array(idle_sheet.convert("RGBA"))
    path = UNITY / "E_Walk.png"
    with Image.open(path) as walk_sheet:
        walk = np.array(walk_sheet.convert("RGBA"))
    for direction in ("Left", "Right"):
        row = ROW_NAMES.index(direction)
        forward = -1 if direction == "Left" else 1
        tile = idle[row * CELL:(row + 1) * CELL, :CELL]
        alpha = tile[..., 3] > 0
        hip = 60 - SIDE_LEG_ROWS
        band = band_of(oklab(tile[..., :3]))
        leg_mask = alpha & (band != 1)
        leg_mask[:hip + 1] = False
        near = np.zeros_like(tile)
        near[leg_mask] = tile[leg_mask]
        far = near.copy()
        shaded = oklab(far[leg_mask][:, :3]) * np.array([FAR_LEG_SHADE, 1, 1])
        nearest = ((shaded[:, None, :] - reference.palette_lab[None]) ** 2).sum(-1).argmin(1)
        far[leg_mask, :3] = reference.palette[nearest]
        upper = tile.copy()
        upper[leg_mask] = 0
        for column in range(6):
            frame = np.zeros_like(tile)
            rise = SIDE_RISE[column]
            for leg, (offset, lift) in ((far, SIDE_FAR[column]), (near, SIDE_NEAR[column])):
                moved = swing_leg(leg, hip, forward * offset, lift, rise)
                filled = moved[..., 3] > 0
                frame[filled] = moved[filled]
            body = np.roll(upper, -rise, axis=0) if rise else upper
            filled = body[..., 3] > 0
            frame[filled] = body[filled]
            labels, count = ndimage.label(frame[..., 3] > 0, structure=np.ones((3, 3)))
            sizes = ndimage.sum(frame[..., 3] > 0, labels, range(1, count + 1))
            frame[np.isin(labels, np.flatnonzero(sizes < 3) + 1)] = 0
            walk[row * CELL:(row + 1) * CELL, column * CELL:(column + 1) * CELL] = frame
    Image.fromarray(walk).save(path)


def hit_cells():
    # This generated sheet has wider outer margins than inner gutters, so the
    # columns are owned by the actual gaps rather than equal thirds.
    source = CONCEPTS / "Hit_B_fourDirections.png"
    with Image.open(source) as image:
        if image.size != (1536, 1024):
            raise ValueError("Hit concept layout changed; recheck x_cuts")
    return cells(source, 3, 4, x_cuts=[0, 583, 945, 1536])


def main():
    for folder in (UNITY, MAGENTA, PREVIEW, FRAMES, FRAMES_MAGENTA, MASTER_DERIVED):
        folder.mkdir(parents=True, exist_ok=True)
    # A single master pose per direction becomes the stable four-frame Idle.
    directions = cells(CONCEPTS / "IdleFacing_B_fourDirections.png", 4, 1)[0]
    place_sheet(UNITY / "E_Idle.png", [[pose] * 4 for pose in directions])
    with Image.open(UNITY / "E_Idle.png") as idle:
        master = Image.new("RGBA", (CELL * 4, CELL))
        for row, direction in enumerate(ROW_NAMES):
            frame = idle.crop((0, row * CELL, CELL, (row + 1) * CELL))
            master.alpha_composite(frame, (row * CELL, 0))
        master.save(UNITY / "E_Master.png")
    heads = idle_heads(UNITY / "E_Idle.png")
    proportions = idle_proportions(UNITY / "E_Idle.png")

    # Walk is composed from Idle later (compose_front_walks / compose_side_walks); start blank.
    Image.new("RGBA", (6 * CELL, 4 * CELL)).save(UNITY / "E_Walk.png")

    attack = cells(CONCEPTS / "Attack_B_fourDirections.png", 6, 4)
    attack[0] = cells(CONCEPTS / "Attack_B_DownFront.png", 6, 1)[0]
    place_sheet(UNITY / "E_Attack.png", attack, heads=heads, idle=proportions)

    place_sheet(UNITY / "E_Hit.png", hit_cells(), heads=heads, idle=proportions)
    place_sheet(UNITY / "E_Death.png", cells(CONCEPTS / "Death_B_Down.png", 8, 1), heads=heads[:1])
    place_sheet(UNITY / "E_Skill.png", cells(CONCEPTS / "Skill_B_Down.png", 6, 1), heads=heads[:1])
    # Props change each Equipment pose's bounds; the shared scale comes from the
    # bare first pose so the E itself stays the same size in all seven.
    place_sheet(UNITY / "E_Equipment.png", cells(CONCEPTS / "Equipment_B_Down.png", 7, 1), heads=heads[:1])

    # Each sheet is matched to the A master on its own, so a concept drawn a
    # little lighter or more orange still lands on the same A color ramps.
    reference = Reference(A_MASTER)
    for name in ("Master", "Idle", "Walk", "Attack", "Hit", "Death", "Skill", "Equipment"):
        restore_a_colors(UNITY / f"E_{name}.png", reference)
    fit_attack_down_legs()
    print("bottom bar matched:", match_down_bars(heads))
    compose_front_walks()
    compose_side_walks(reference)
    # Master is Idle frame 1 per direction; rebuild it from the final Idle.
    with Image.open(UNITY / "E_Idle.png") as idle:
        master = Image.new("RGBA", (CELL * 4, CELL))
        for row in range(4):
            master.alpha_composite(idle.crop((0, row * CELL, CELL, (row + 1) * CELL)), (row * CELL, 0))
        master.save(UNITY / "E_Master.png")

    with Image.open(UNITY / "E_Master.png") as master:
        for column, direction in enumerate(ROW_NAMES):
            frame = master.crop((column * CELL, 0, (column + 1) * CELL, CELL))
            magenta(frame).save(MASTER_DERIVED / f"E_Master_{direction}.png")
        magenta(master).save(MASTER_DERIVED / "E_Master_DirectionSheet.png")
        magenta(master).resize((2048, 512), Image.Resampling.NEAREST).save(
            MASTER_DERIVED / "E_Master_DirectionSheet_x8.png")

    # Master's derived copies live in ArtSource/E_Master/64px (written above).
    for name, grid in (("Idle", (4, 4)),
                       ("Walk", (6, 4)), ("Attack", (6, 4)),
                       ("Hit", (3, 4)), ("Death", (8, 1)),
                       ("Skill", (6, 1)), ("Equipment", (7, 1))):
        write(name, grid)


if __name__ == "__main__":
    main()

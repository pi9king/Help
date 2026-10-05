"""Cut concept poses out of a magenta-key sheet without neighbor bleed.

Equal grid slicing let neighboring poses leak into a cell (slivers at the cell
edge) and cut a pose's own cape off when it crossed the grid line. Instead, the
whole sheet is split into connected pieces and each piece goes to the cell that
contains its centroid. Requires Pillow, numpy and scipy.
"""

import numpy as np
from PIL import Image
from scipy import ndimage

# Pieces smaller than this (source pixels) are keying noise, not sparks/debris.
MIN_PIECE = 12
DESPILL_PASSES = 4


def is_magenta_tinted(rgb):
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    return (r > g + 25) & (b > g + 25) & (b * 100 > r * 55)


def is_background(rgb):
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    return (r > 185) & (b > 145) & (g < 140) & (r * 10 > g * 15) & (b * 10 > g * 14)


def foreground_mask(rgb):
    mask = ~is_background(rgb)
    tinted = is_magenta_tinted(rgb)
    # Anti-aliased key pixels survive the hard threshold as a magenta rim.
    # Peel only tinted pixels that touch the background, a few layers deep.
    for _ in range(DESPILL_PASSES):
        edge = mask & ~ndimage.binary_erosion(mask, structure=np.ones((3, 3)), border_value=0)
        peel = edge & tinted
        if not peel.any():
            break
        mask &= ~peel
    return mask


def despill_sheet(image):
    """Drop magenta rim pixels left on an already cut 64px sheet."""
    pixels = np.array(image.convert("RGBA"))
    mask = pixels[..., 3] > 0
    tinted = is_magenta_tinted(pixels[..., :3])
    for _ in range(DESPILL_PASSES):
        edge = mask & ~ndimage.binary_erosion(mask, structure=np.ones((3, 3)), border_value=0)
        peel = edge & tinted
        if not peel.any():
            break
        mask &= ~peel
    pixels[~mask] = 0
    return Image.fromarray(pixels)


def gap_cuts(occupied, count):
    """Cut where the sheet is actually empty, nearest to the equal-grid lines.

    Generated sheets do not space poses evenly (Death's fourth pose sits over the
    third grid cell), so equal cuts hand a pose to the wrong cell.
    """
    length = len(occupied)
    empty = np.flatnonzero(~occupied)
    runs = np.split(empty, np.flatnonzero(np.diff(empty) != 1) + 1) if empty.size else []
    centers = np.array([run.mean() for run in runs if run.size])
    cuts = [0]
    for k in range(1, count):
        expected = k * length / count
        if centers.size:
            nearest = centers[np.abs(centers - expected).argmin()]
            if abs(nearest - expected) < length / count / 2:
                expected = nearest
        cuts.append(int(round(expected)))
    cuts.append(length)
    return cuts


def cells(path, columns, rows, x_cuts=None, y_cuts=None):
    with Image.open(path) as source:
        rgb = np.array(source.convert("RGB"))
    height, width = rgb.shape[:2]
    mask = foreground_mask(rgb)
    labels, count = ndimage.label(mask, structure=np.ones((3, 3)))
    sizes = ndimage.sum(mask, labels, range(1, count + 1))
    solid = np.isin(labels, np.flatnonzero(sizes >= MIN_PIECE) + 1)
    y_cuts = y_cuts or gap_cuts(solid.any(axis=1), rows)
    x_cuts = x_cuts or gap_cuts(solid.any(axis=0), columns)
    centers = ndimage.center_of_mass(mask, labels, range(1, count + 1))
    slices = ndimage.find_objects(labels)

    # Each piece goes whole to the cell holding its centroid. Poses whose capes
    # touch (Death) merge into one piece spanning several cells; only those are
    # divided along the grid lines.
    column_of = np.searchsorted(x_cuts, np.arange(width), side="right") - 1
    row_of = np.searchsorted(y_cuts, np.arange(height), side="right") - 1
    grid_cell = row_of[:, None] * columns + column_of[None, :]
    owner = np.full(count + 1, -1)
    owner_map = np.full((height, width), -1)
    for index, (size, (cy, cx), box) in enumerate(zip(sizes, centers, slices), 1):
        if size < MIN_PIECE:
            continue
        spans_cells = (box[1].stop - box[1].start > 1.25 * width / columns
                       or box[0].stop - box[0].start > 1.25 * height / rows)
        if spans_cells:
            piece = labels[box] == index
            owner_map[box][piece] = grid_cell[box][piece]
        else:
            col = int(np.searchsorted(x_cuts, cx, side="right")) - 1
            row = int(np.searchsorted(y_cuts, cy, side="right")) - 1
            owner[index] = row * columns + col
    whole = owner[labels]
    owner_map = np.where(whole >= 0, whole, owner_map)

    rgba = np.dstack([rgb, np.full((height, width), 255, np.uint8)])
    grid = []
    for row in range(rows):
        frames = []
        for col in range(columns):
            keep = owner_map == row * columns + col
            ys, xs = np.nonzero(keep)
            if not ys.size:
                raise ValueError(f"Empty concept cell: {path} row={row} col={col}")
            y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
            crop = rgba[y0:y1, x0:x1].copy()
            crop[~keep[y0:y1, x0:x1]] = 0
            frames.append(Image.fromarray(crop))
        grid.append(frames)
    return grid

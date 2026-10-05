"""Give the B sheets the colors of the original (A) E master.

A plain nearest-palette mapping kept B's own value distribution, so the armor
stayed near-black and the cape stayed scarlet. Instead each material band
(neutral armor, red cape, gold trim, cyan rune) is histogram-matched: a B pixel
at the n-th lightness percentile of its band takes the A color found at the same
percentile of the same band, snapped to A's actual colors. Requires numpy.
"""

import numpy as np
from PIL import Image

from e_sprite_cleanup import is_magenta_tinted

# Hue ranges (OkLCh degrees). The red/gold valley sits at ~33 degrees in every sheet.
BANDS = (("neutral", None), ("red", (345, 33)), ("gold", (33, 120)),
         ("cyan", (120, 300)), ("pink", (300, 345)))
CHROMA_MIN = 0.05
QUANTILES = 128


def oklab(rgb):
    c = np.asarray(rgb, float) / 255.0
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    m1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929],
                   [0.2119034982, 0.6806995451, 0.1073969566],
                   [0.0883024619, 0.2817188376, 0.6299787005]])
    m2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468],
                   [1.9779984951, -2.4285922050, 0.4505937099],
                   [0.0259040371, 0.7827717662, -0.8086757660]])
    return np.cbrt(c @ m1.T) @ m2.T


def band_of(lab):
    chroma = np.hypot(lab[..., 1], lab[..., 2])
    hue = np.degrees(np.arctan2(lab[..., 2], lab[..., 1])) % 360
    band = np.zeros(lab.shape[:-1], int)
    for index, (_, (lo, hi)) in enumerate(BANDS[1:], 1):
        inside = (hue >= lo) | (hue < hi) if lo > hi else (hue >= lo) & (hue < hi)
        band[(chroma > CHROMA_MIN) & inside] = index
    return band


def opaque_colors(path):
    with Image.open(path) as image:
        pixels = np.array(image.convert("RGBA")).reshape(-1, 4)
    rgb = pixels[pixels[:, 3] > 128][:, :3]
    return rgb[~is_magenta_tinted(rgb)]


class Reference:
    def __init__(self, path):
        rgb = opaque_colors(path)
        self.lab = oklab(rgb)
        self.band = band_of(self.lab)
        self.palette = np.unique(rgb, axis=0)
        self.palette_lab = oklab(self.palette)
        self.palette_band = band_of(self.palette_lab)


def recolor(image, reference):
    pixels = np.array(image.convert("RGBA"))
    flat = pixels.reshape(-1, 4)
    visible = flat[:, 3] > 0
    lab = oklab(flat[visible][:, :3])
    band = band_of(lab)
    out = flat[visible][:, :3].copy()
    steps = np.linspace(0, 1, QUANTILES + 1)
    for index in range(len(BANDS)):
        mine = band == index
        theirs = reference.band == index
        if mine.sum() < 10 or theirs.sum() < 10:
            continue
        source_q = np.quantile(lab[mine, 0], steps)
        ordered = reference.lab[theirs][np.argsort(reference.lab[theirs, 0])]
        window = max(2, len(ordered) // QUANTILES)
        targets = np.array([
            ordered[max(0, k - window):k + window + 1].mean(0)
            for k in (steps * (len(ordered) - 1)).astype(int)])
        position = np.interp(lab[mine, 0], source_q, steps) * QUANTILES
        lo = np.floor(position).astype(int)
        hi = np.minimum(lo + 1, QUANTILES)
        frac = (position - lo)[:, None]
        wanted = targets[lo] * (1 - frac) + targets[hi] * frac
        candidates = reference.palette_band == index
        if candidates.sum() < 4:
            candidates[:] = True
        distance = ((wanted[:, None, :] - reference.palette_lab[candidates][None]) ** 2).sum(-1)
        out[mine] = reference.palette[candidates][distance.argmin(1)]
    flat = flat.copy()
    flat[visible, :3] = out
    return Image.fromarray(flat.reshape(pixels.shape))

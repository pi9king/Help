# 플레이어 실루엣 습작 — "E를 그리지 않고 E가 드러나게" 할 수 있는가.
#
# 앞선 초안(gen_player_drafts.py)은 E를 몸통으로 놓고 도적 장비를 붙였다.
# 글자가 대놓고 박혀 보였고, 같은 발상으로 쓴 프롬프트는 AI에서도 똑같이 나왔다.
#
# 여기서는 반대로 간다: **도적의 자세를 그리고, E는 거기서 저절로 생기게 한다.**
# 깊게 웅크린 런지 자세에는 이미 E가 들어 있다 —
#   세로획 = 곧게 편 등 + 뒤로 흐르는 망토
#   윗대   = 앞으로 뻗은 팔 + 단검
#   가운뎃대 = 가슴 앞에 접은 반대쪽 팔뚝 (짧다)
#   아랫대 = 앞으로 딛은 다리의 정강이
#
# strength(0.0~1.0)는 "얼마나 대놓고 E인가" 다이얼이다.
#   0.0 = 그냥 도적. 알아채기 어렵다.
#   1.0 = 자세가 과장돼 E가 또렷하다. 대신 자세가 부자연스러워진다.
# 켜고 끄는 스위치가 아니라 눈금이라, 실물을 보고 고르는 수밖에 없다.
#
# 실행: python Tools/gen_player_pose.py
# 결과: ArtSource/preview/pose_{weak,mid,strong}_x8.png (+ 16px 축소 테스트)

import io
import os
import struct
import zlib

W, H = 32, 48

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PREVIEW_DIR = os.path.join(ROOT, "ArtSource", "preview")

BODY = (26, 32, 44)        # 실루엣 한 톤 — 형태만 본다
EYE = (0, 229, 255)
BG = (0, 0, 0, 0)


def blank():
    return [[None] * W for _ in range(H)]


def span(g, y, x0, x1, color=BODY):
    if y < 0 or y >= H:
        return
    for x in range(max(0, x0), min(W, x1 + 1)):
        g[y][x] = color


def lerp(a, b, t):
    return int(round(a + (b - a) * t))


def build(strength):
    """t=0 자연스러운 도적 / t=1 자세를 과장해 E를 또렷하게.

    핵심: **가로대 사이를 비워야 E가 읽힌다.**
    몸통을 통짜로 채우면 아무리 팔다리를 뻗어도 그냥 덩어리로 보인다.
    그래서 몸은 가늘게(망토가 곧게 떨어지는 후드 도적이라 자연스럽다),
    팔·팔뚝·정강이만 앞으로 내밀고 그 사이는 완전히 비운다.

    세로 배분:
      0-7   아랫대(앞 정강이)   8-16  빈칸(몸통만)
      17-22 가운뎃대(팔뚝)      23-30 빈칸(몸통만)
      31-36 윗대(뻗은 팔+단검)  37-47 어깨·후드
    """
    t = strength
    g = blank()

    # 뒷선(세로획). 곧은 수직선 하나가 E 인식의 절반을 한다.
    back = lerp(4, 2, t)
    # 몸통 앞선. 얇을수록 가로대가 또렷해진다.
    front = lerp(12, 9, t)

    # --- 후드 -------------------------------------------------------------
    hood = [
        (47, 7, 11), (46, 6, 13), (45, 5, 15), (44, 4, 16),
        (43, back, 17), (42, back, 17), (41, back, 17),
        (40, back, 16), (39, back, 15), (38, back, 14),
    ]
    for y, x0, x1 in hood:
        span(g, y, min(x0, back), x1)

    # 어깨
    span(g, 37, back, lerp(14, 12, t))

    # --- 윗대: 앞으로 뻗은 팔 + 단검 ----------------------------------------
    # 약: 팔이 아래로 처져 자연스럽다. 강: 어깨 높이에서 수평으로 곧게 뻗는다.
    arm = lerp(19, 25, t)
    span(g, 36, back, arm)
    span(g, 35, back, arm + 1)
    span(g, 34, back, lerp(17, 24, t))
    span(g, 33, back, lerp(15, 22, t))
    span(g, 32, back, lerp(13, 19, t))
    span(g, 31, back, lerp(front, 15, t))

    # 단검 — 팔의 연장. 강할수록 수평으로 길게 뻗어 윗대를 완성한다.
    blade = lerp(23, 30, t)
    span(g, 36, arm, blade)
    span(g, 35, arm, blade)

    # --- 빈칸 (몸통만) -----------------------------------------------------
    for y in range(23, 31):
        span(g, y, back, front)

    # --- 가운뎃대: 가슴 앞에 접은 팔뚝 --------------------------------------
    # E의 가운뎃대는 짧다. 팔뚝이라 자연히 짧아진다 — 억지가 아니다.
    fore = lerp(15, 20, t)
    span(g, 22, back, fore - 2)
    span(g, 21, back, fore)
    span(g, 20, back, fore)
    span(g, 19, back, fore - 1)
    span(g, 18, back, lerp(13, 16, t))
    span(g, 17, back, lerp(front, 13, t))

    # --- 빈칸 (허리·접은 뒷다리) --------------------------------------------
    for y in range(8, 17):
        span(g, y, back, front)

    # --- 아랫대: 앞으로 딛은 다리의 정강이 ----------------------------------
    # 깊은 런지에서 앞 정강이는 거의 수평이 된다 — 실제 자세다.
    shin = lerp(17, 24, t)
    span(g, 7, back, lerp(14, 18, t))
    span(g, 6, back, lerp(16, 22, t))
    span(g, 5, back, shin)
    span(g, 4, back - 1, shin)
    span(g, 3, back - 1, shin)
    span(g, 2, back - 2, lerp(16, 22, t))
    span(g, 1, back - 2, lerp(14, 19, t))
    span(g, 0, back - 1, lerp(12, 16, t))

    # --- 망토 --------------------------------------------------------------
    # 뒷선을 아래까지 곧게 이어 세로획을 완성한다.
    for y in range(4, 38):
        span(g, y, max(0, back - 2), back)

    # --- 눈 ----------------------------------------------------------------
    # 얼굴은 없다. 후드 그늘 속 가느다란 두 줄만.
    span(g, 42, 10, 11, EYE)
    span(g, 41, 13, 14, EYE)

    return g


# ---------------------------------------------------------------- PNG

def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def to_rows(g):
    rows = []
    for y in range(H - 1, -1, -1):
        line = b""
        for x in range(W):
            c = g[y][x]
            line += bytes((0, 0, 0, 0)) if c is None else bytes((c[0], c[1], c[2], 255))
        rows.append(line)
    return rows


def write_png(path, rows, width, height):
    raw = b""
    for line in rows:
        raw += b"\x00" + line
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    d = os.path.dirname(path)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    io.open(path, "wb").write(png)


def scale(rows, width, height, factor):
    out = []
    for line in rows:
        big = b""
        for x in range(width):
            big += line[x * 4:(x + 1) * 4] * factor
        for _ in range(factor):
            out.append(big)
    return out, width * factor, height * factor


def shrink_half(rows, width, height):
    nw, nh = width // 2, height // 2
    out = []
    for y in range(nh):
        line = b""
        for x in range(nw):
            best = bytes((0, 0, 0, 0))
            for dy in (0, 1):
                for dx in (0, 1):
                    px = rows[y * 2 + dy][(x * 2 + dx) * 4:(x * 2 + dx) * 4 + 4]
                    if px[3] != 0:
                        best = px
                        break
                if best[3] != 0:
                    break
            line += best
        out.append(line)
    return out, nw, nh


def emit(name, strength):
    rows = to_rows(build(strength))
    big, bw, bh = scale(rows, W, H, 8)
    write_png(os.path.join(PREVIEW_DIR, name + "_x8.png"), big, bw, bh)

    small, sw, sh = shrink_half(rows, W, H)
    tiny, tw, th = scale(small, sw, sh, 8)
    write_png(os.path.join(PREVIEW_DIR, name + "_squint.png"), tiny, tw, th)
    print("%-14s strength=%.1f" % (name, strength))


if __name__ == "__main__":
    emit("pose_weak", 0.0)
    emit("pose_mid", 0.5)
    emit("pose_strong", 1.0)

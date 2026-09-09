# 플레이어(알파벳 E = 도적) 스프라이트 초안 생성기.
#
# 32x48, PPU 32 기준 = 화면에서 1 x 1.5칸.
#
# 설계 원칙 (Docs/ART_PIPELINE.md 3-2절):
#   1. 얼굴을 그리지 않는다 — 후드 그림자 속 빛나는 눈 두 점만.
#      글자 캐릭터가 유아용으로 보이는 건 거의 전부 얼굴 때문이다.
#   2. E는 장식하는 대상이 아니라 몸통이다 — 세로 기둥 = 몸, 가로대 3개 = 어깨/팔/다리.
#      후드와 망토는 그 위에 덮는 장비.
#   3. 32px에서는 명암 덩어리만 남는다. 가운뎃대를 짧게 유지해야 E로 읽힌다.
#
# 실행: python Tools/gen_player_drafts.py
# 결과: Assets/Sprites/player_E_draftA.png, player_E_draftB.png
#       ArtSource/preview/*.png  (x8 확대 + 16px 축소 가독성 테스트)

import io
import os
import struct
import zlib

W, H = 32, 48

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SPRITE_DIR = os.path.join(ROOT, "Assets", "Sprites")
PREVIEW_DIR = os.path.join(ROOT, "ArtSource", "preview")

TRANSPARENT = "."
OUTLINE = "#"

# 몸통으로 취급하는 문자 — 림라이트(B안)와 음영 계산 대상
BODY = "XLMN"


# ---------------------------------------------------------------- 격자 도구

def blank():
    """grid[y][x], y=0이 바닥."""
    return [[TRANSPARENT] * W for _ in range(H)]


def rect(g, x0, y0, x1, y1, ch):
    for y in range(max(0, y0), min(H, y1 + 1)):
        for x in range(max(0, x0), min(W, x1 + 1)):
            g[y][x] = ch


def row(g, y, x0, x1, ch):
    rect(g, x0, y, x1, y, ch)


def dump(g, title):
    """콘솔로 눈 검사. 픽셀아트는 코드보다 그림으로 봐야 틀린 게 보인다."""
    print("\n== %s ==" % title)
    for y in range(H - 1, -1, -1):
        print("".join(g[y]))


# ---------------------------------------------------------------- 형태

def build_geometry():
    """A안과 B안이 공유하는 실루엣. 색만 다르게 입힌다.

    세로 배분 (48px):
      0-4   발(부츠)      5-11  아랫대      12-17 빈칸
      18-24 가운뎃대      25-30 빈칸        31-37 윗대
      37-47 후드
    """
    g = blank()

    # --- E 본체 -----------------------------------------------------------
    # 세로 기둥 = 몸통. 가로대 3개 = 어깨 / 단검 든 팔 / 다리.
    rect(g, 6, 5, 11, 37, "L")
    rect(g, 12, 31, 25, 37, "L")   # 윗대 (길다)
    rect(g, 12, 18, 19, 24, "L")   # 가운뎃대 (짧다 — E 인식의 핵심 + 단검 자리 확보)
    rect(g, 12, 5, 25, 11, "L")    # 아랫대 (길다)

    # 명암 4단계로 부피를 준다. 빛은 왼쪽 위에서.
    # 이게 없으면 밋밋한 색종이처럼 보인다.
    rect(g, 6, 5, 6, 37, "X")      # 기둥 왼쪽 = 빛나는 면
    rect(g, 11, 5, 11, 30, "M")    # 기둥 오른쪽 = 그늘
    row(g, 37, 12, 25, "X")        # 각 가로대 윗면 = 빛
    row(g, 24, 12, 19, "X")
    row(g, 11, 12, 25, "X")
    row(g, 31, 12, 25, "M")        # 각 가로대 밑면 = 그늘
    row(g, 18, 12, 19, "M")
    row(g, 5, 12, 25, "M")
    rect(g, 25, 31, 25, 36, "M")   # 가로대 끝면
    rect(g, 25, 6, 25, 10, "M")
    rect(g, 19, 19, 19, 23, "M")

    # --- 장비는 전부 어두운 한 계열 -----------------------------------------
    # 후드·망토·부츠·허리끈·손잡이를 같은 재질로 묶으면
    # "밝은 글자 몸 + 어두운 장비"라는 대비 하나로 그림 전체가 정리된다.
    # 재료가 셋 이상 되면 32px에서 죽처럼 뭉갠다.

    # 부츠 — 몸에서 이어지는 게 아니라 신은 것으로 읽혀야 한다
    rect(g, 7, 0, 10, 4, "H")
    rect(g, 18, 0, 21, 4, "H")
    row(g, 0, 6, 11, "H")
    row(g, 0, 17, 22, "H")

    # 후드 — 뒤로 젖혀진 뾰족한 봉우리 + 앞으로 튀어나온 차양.
    # 둥글면 투구가 되고, 차양이 없으면 눈이 그냥 얼굴이 된다.
    hood = [
        (47, 6, 8), (46, 5, 10), (45, 4, 12), (44, 4, 14),
        (43, 3, 16), (42, 3, 18), (41, 3, 19), (40, 3, 19),
        (39, 4, 18), (38, 4, 17), (37, 5, 16),
    ]
    for y, x0, x1 in hood:
        row(g, y, x0, x1, "H")

    # 차양 아래 어둠. 얼굴은 그리지 않는다 — 이 어둠이 얼굴 자리를 대신한다.
    row(g, 41, 12, 18, "h")
    row(g, 40, 11, 18, "h")
    row(g, 39, 12, 17, "h")

    # 어둠 속 눈 두 점. 가늘고(1px 높이) 앞으로 갈수록 낮아진다 —
    # 이 기울기가 '노려봄'을 만든다. 동그란 눈 두 개면 그 순간 마스코트가 된다.
    rect(g, 13, 41, 14, 41, "E")
    rect(g, 16, 40, 17, 40, "E")

    # --- 망토 -------------------------------------------------------------
    # 후드 밑동에서 이어져 뒤로 흘러내린다. 아래를 너덜하게 끊어 실루엣을 깬다.
    rect(g, 2, 14, 5, 38, "C")
    rect(g, 1, 20, 1, 33, "C")
    row(g, 13, 2, 4, "C")
    row(g, 12, 2, 3, "C")
    row(g, 11, 3, 3, "C")
    row(g, 13, 5, 5, "C")
    row(g, 12, 5, 5, "C")
    row(g, 10, 4, 5, "C")
    row(g, 9, 4, 4, "C")

    # --- 허리끈 -----------------------------------------------------------
    # 기둥 안쪽에만 두른다. 밖으로 튀어나오면 떠 있는 판자로 보인다.
    rect(g, 7, 27, 11, 28, "H")
    rect(g, 9, 26, 10, 26, "G")    # 작은 금색 버클 — 유일한 금색 포인트 둘 중 하나

    # --- 단검 -------------------------------------------------------------
    # 가운뎃대(=팔) 끝에서 아래로 비스듬히. 빈 공간(가운뎃대~아랫대 사이)을 채우고
    # E의 가로대와 겹치지 않아 글자 인식을 방해하지 않는다.
    rect(g, 20, 18, 21, 20, "H")   # 손잡이 (장비 = 어두운 계열)
    rect(g, 22, 16, 22, 21, "H")   # 코등이 — 이게 있어야 '칼'로 읽힌다

    # 날은 위 모서리만 밝게, 몸통은 어둡게. 한 톤으로 칠하면 종잇조각이 된다.
    blade = [
        (23, 15, 18), (24, 15, 17), (25, 14, 17),
        (26, 14, 16), (27, 13, 15), (28, 13, 14),
    ]
    for x, y0, y1 in blade:
        rect(g, x, y0, x, y1, "s")
        row(g, y1, x, x, "S")      # 날 등이 빛을 받는다
    rect(g, 29, 13, 29, 13, "s")   # 끝
    rect(g, 27, 15, 28, 14, "G")   # 날 끝 반짝임

    return g


# ---------------------------------------------------------------- 후처리

def add_outline(g):
    """실루엣 바깥 1픽셀. 어두운 배경에서 캐릭터가 묻히지 않게 한다."""
    out = [r[:] for r in g]
    for y in range(H):
        for x in range(W):
            if g[y][x] != TRANSPARENT:
                continue
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    if dx == 0 and dy == 0:
                        continue
                    ny, nx = y + dy, x + dx
                    if 0 <= ny < H and 0 <= nx < W and g[ny][nx] != TRANSPARENT:
                        out[y][x] = OUTLINE
                        break
                if out[y][x] == OUTLINE:
                    break
    return out


def add_rimlight(g):
    """B안 전용 — 몸통의 위/오른쪽 모서리를 빛나게 한다.

    몸 전체가 어두우면 어두운 던전에서 플레이어가 사라진다.
    테두리만 밝혀서 '그림자 속 존재'를 유지하면서 가독성을 지킨다.
    """
    out = [r[:] for r in g]
    for y in range(H):
        for x in range(W):
            if g[y][x] not in BODY:
                continue
            above = g[y + 1][x] if y + 1 < H else TRANSPARENT
            right = g[y][x + 1] if x + 1 < W else TRANSPARENT
            if above not in BODY or right not in BODY:
                out[y][x] = "R"
    return out


# ---------------------------------------------------------------- 색

def hexes(**kw):
    return {k: (int(v[0:2], 16), int(v[2:4], 16), int(v[4:6], 16)) for k, v in kw.items()}


# A안 — 밝은 E 몸 + 어두운 후드. 글자가 가장 또렷하고 배경에서 잘 튄다.
PALETTE_A = hexes(
    X="FFFFFF", L="D8E4EE", M="9AB0C4", N="7FA0B8",   # 뼈빛 석판 4단계
    H="182634", h="0A1119",      # 후드 / 그 속의 어둠
    C="3A2A4E",                  # 망토
    E="00E5FF",                  # 눈
    s="7FA0B8", S="FFFFFF", G="FFCC00", W="6A4A2E",
)
PALETTE_A[OUTLINE] = (16, 24, 32)

# B안 — 그림자 E + 시안 림라이트. 더 암살자답지만 가독성을 테두리에만 의존한다.
PALETTE_B = hexes(
    X="283848", L="182634", M="101820", N="0A1119",
    H="0A1119", h="0A1119",
    C="1E3350",
    E="00E5FF",
    s="3A506E", S="7FA0B8", G="FFCC00", W="3A2A1E",
    R="00E5FF",                  # 림라이트
)
PALETTE_B[OUTLINE] = (10, 17, 25)


def to_pixels(g, palette):
    """grid → RGBA 바이트 (위에서 아래로, PNG 순서)."""
    rows = []
    for y in range(H - 1, -1, -1):
        line = b""
        for x in range(W):
            ch = g[y][x]
            if ch == TRANSPARENT:
                line += bytes((0, 0, 0, 0))
            else:
                r, gg, b = palette[ch]
                line += bytes((r, gg, b, 255))
        rows.append(line)
    return rows


# ---------------------------------------------------------------- PNG

def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def write_png(path, rows, width, height):
    raw = b""
    for line in rows:
        raw += b"\x00" + line   # 필터 없음
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")

    directory = os.path.dirname(path)
    if directory and not os.path.isdir(directory):
        os.makedirs(directory)
    io.open(path, "wb").write(png)


def scale(rows, width, height, factor):
    """최근접 확대 — 확인용. 보간하면 픽셀이 뭉개져서 볼 이유가 없어진다."""
    out = []
    for line in rows:
        big = b""
        for x in range(width):
            big += line[x * 4:(x + 1) * 4] * factor
        for _ in range(factor):
            out.append(big)
    return out, width * factor, height * factor


def shrink_half(rows, width, height):
    """가독성 테스트용 절반 축소(16x24). 눈을 가늘게 뜨고 봐도 E로 읽혀야 한다."""
    nw, nh = width // 2, height // 2
    out = []
    for y in range(nh):
        line = b""
        for x in range(nw):
            # 2x2에서 불투명한 것 중 첫 번째를 고른다(최빈색 축소의 간이판)
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


# ---------------------------------------------------------------- 실행

def emit(name, grid, palette, show=False):
    if show:
        dump(grid, name)

    rows = to_pixels(grid, palette)
    write_png(os.path.join(SPRITE_DIR, name + ".png"), rows, W, H)

    big, bw, bh = scale(rows, W, H, 8)
    write_png(os.path.join(PREVIEW_DIR, name + "_x8.png"), big, bw, bh)

    small, sw, sh = shrink_half(rows, W, H)
    tiny, tw, th = scale(small, sw, sh, 8)
    write_png(os.path.join(PREVIEW_DIR, name + "_squint.png"), tiny, tw, th)

    print("%-22s 32x48  → Assets/Sprites/%s.png" % (name, name))


def main():
    geo = build_geometry()

    emit("player_E_draftA", add_outline(geo), PALETTE_A, show=True)
    emit("player_E_draftB", add_outline(add_rimlight(geo)), PALETTE_B)


if __name__ == "__main__":
    main()

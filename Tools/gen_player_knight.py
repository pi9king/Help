# 플레이어 캐릭터 — "다크나이트" 방향.
#
# 앞선 도적 초안이 음침했던 이유는 어두워서가 아니라 자세였다:
#   웅크림 / 앞으로 굽음 / 가늘고 비대칭 / 너덜한 천 → 비열해 보인다.
# 다크나이트는 어둡되 위압적이다:
#   꼿꼿이 섬 / 어깨가 넓음 / 정돈된 갑주 / 대칭 / 명암이 또렷함.
#
# 무기는 그리지 않는다. 이 게임은 무기를 계속 갈아끼우는 게 핵심 루프라
# 스프라이트에 특정 무기를 박아 넣으면 설계가 어긋난다. 손은 비워 둔다.
#
# E는 **가슴 문장**으로 단다 — 배트맨이 가슴에 박쥐를 다는 그 자리다.
# 활자를 붙이는 게 아니라 문장으로 디자인해야 하므로, 세 줄을 살짝 기울이고
# 방패꼴 판 위에 얹어 문양으로 읽히게 한다.
#
# 실행: python Tools/gen_player_knight.py
# 결과: Assets/Sprites/player_E_knight.png (32x48)
#       ArtSource/preview/knight_x8.png, knight_squint.png, knight_silhouette_x8.png

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


def blank():
    return [[TRANSPARENT] * W for _ in range(H)]


def rect(g, x0, y0, x1, y1, ch):
    for y in range(max(0, y0), min(H, y1 + 1)):
        for x in range(max(0, x0), min(W, x1 + 1)):
            g[y][x] = ch


def row(g, y, x0, x1, ch):
    rect(g, x0, y, x1, y, ch)


def build(step=4):
    """다크나이트. step은 개선을 하나씩 얹기 위한 눈금이다.

      0 = 기준 (대칭·단색·평평)
      1 = 망토를 한쪽 어깨에 걸치고 앞으로 쓸어 넘긴다  → 실루엣의 서명
      2 = 망토 안감을 진홍으로                          → 유일한 보색
      3 = 금색을 죽이고 바이저를 가장 밝게              → 시선의 초점
      4 = 대칭을 1~2픽셀 깬다                           → 마네킹에서 사람으로

    세로 배분 (48px):
      0-4 부츠 / 5-21 다리 / 22-23 허리띠 / 24-36 몸통 / 37-47 투구
    """
    g = blank()
    # 머리까지 함께 밀었더니 목이 어긋나 보였다. 기울임은 무게중심(뒷굽)으로만 준다.
    lean = 0

    # --- 망토 (몸 뒤) --------------------------------------------------------
    # 어깨에서 좁게 시작해 아래로 넓어지는 사다리꼴. 바깥선을 비스듬히 깎아
    # '흘러내린다'로 읽히게 한다. 수직 벽이면 망토가 아니라 담벼락이다.
    cape = [
        (37, 6, 10), (36, 6, 10), (35, 5, 10), (34, 5, 10), (33, 5, 10),
        (32, 5, 10), (31, 4, 10), (30, 4, 10), (29, 4, 10), (28, 4, 10),
        (27, 3, 10), (26, 3, 10), (25, 3, 10), (24, 2, 10), (23, 2, 10),
        (22, 2, 10), (21, 2, 11), (20, 1, 11), (19, 1, 11), (18, 1, 11),
        (17, 1, 11), (16, 0, 11), (15, 0, 11), (14, 0, 12), (13, 0, 12),
        (12, 0, 12), (11, 0, 12), (10, 0, 12), (9, 0, 12), (8, 1, 12),
        (7, 1, 11), (6, 2, 11), (5, 3, 10),
    ]
    for y, x0, x1 in cape:
        row(g, y, x0, x1, "K")

    # 검은 덩어리로 남지 않게: 몸에 닿는 면의 반사광 + 비스듬한 주름.
    # 안감을 망토 전 길이에 칠했더니 옷에 난 세로 솔기처럼 보였다.
    # 실제로 안감이 보이는 곳은 '젖혀진 데'뿐이다 — 어깨 근처와 앞으로 넘긴 자락.
    lining = "R" if step >= 2 else "k"
    for y, x0, x1 in cape:
        row(g, y, x1 - 1, x1, "k" if (step < 2 or y < 28) else lining)
        fold = x0 + (37 - y) // 6 + 1
        if fold < x1 - 2:
            row(g, y, fold, fold, "k")
        fold2 = x0 + (37 - y) // 3 + 3
        if fold2 < x1 - 2:
            row(g, y, fold2, fold2, "k")

    row(g, 5, 6, 7, "K")             # 얕게 물결지는 단
    row(g, 4, 3, 5, "K")
    row(g, 4, 8, 9, "K")

    # --- 투구 ---------------------------------------------------------------
    # 눈두덩이 앞으로 튀어나오고 턱으로 갈수록 좁아진다. 상자면 로봇이 된다.
    helm = [
        (47, 13, 17), (46, 12, 19), (45, 11, 21), (44, 11, 22),
        (43, 11, 23), (42, 11, 22), (41, 11, 21), (40, 12, 20),
        (39, 12, 19), (38, 13, 18), (37, 14, 17),
    ]
    for y, x0, x1 in helm:
        row(g, y, x0 + lean, x1 + lean, "A")
    rect(g, 8, 42, 12, 47, "A")      # 뒤로 뻗은 볏

    row(g, 46, 13 + lean, 17 + lean, "a")
    row(g, 45, 12 + lean, 16 + lean, "a")
    row(g, 44, 11 + lean, 14 + lean, "a")
    row(g, 43, 11 + lean, 13 + lean, "a")

    # --- 면갑 ---------------------------------------------------------------
    void = [(42, 15, 21), (41, 14, 21), (40, 14, 20),
            (39, 14, 19), (38, 15, 18)]
    for y, x0, x1 in void:
        row(g, y, x0 + lean, x1 + lean, "v")

    row(g, 42, 16 + lean, 20 + lean, "E")
    row(g, 40, 15 + lean, 19 + lean, "E")
    row(g, 38, 15 + lean, 17 + lean, "E")

    if step >= 3:
        # 플레이어는 언제나 얼굴을 본다. 거기가 화면에서 가장 밝아야
        # 캐릭터가 '본다'는 느낌이 생기고, 자기 위치를 놓치지 않는다.
        row(g, 42, 17 + lean, 19 + lean, "F")
        row(g, 40, 16 + lean, 18 + lean, "F")
        row(g, 43, 15 + lean, 20 + lean, "n")   # 틈에서 새어 나오는 빛
        row(g, 37, 15 + lean, 18 + lean, "n")

    # --- 어깨 (각진 견갑) ----------------------------------------------------
    # 위압감의 8할. 다만 정사각형이 아니라 위아래로 각을 줘야 갑주로 읽힌다.
    row(g, 36, 9, 21 + lean, "B")
    row(g, 35, 8, 22 + lean, "B")
    row(g, 34, 8, 22 + lean, "B")
    row(g, 33, 9, 21 + lean, "B")
    rect(g, 8, 34, 9, 35, "a")
    rect(g, 21 + lean, 34, 22 + lean, 35, "a")

    if step >= 1:
        rect(g, 7, 35, 9, 37, "K")   # 어깨에 걸친 망토
        rect(g, 7, 36, 8, 36, "G")   # 고정 장식

    # --- 몸통 (좁아진다) -----------------------------------------------------
    torso = [
        (32, 10, 20), (31, 10, 20), (30, 10, 20), (29, 11, 19),
        (28, 11, 19), (27, 11, 19), (26, 11, 18), (25, 12, 18), (24, 12, 18),
    ]
    for y, x0, x1 in torso:
        row(g, y, x0, x1 + lean, "B")
    rect(g, 19 + lean, 25, 20 + lean, 32, "b")

    # --- 가슴 문장 -----------------------------------------------------------
    # 세로 기둥 없이 가로줄 셋만 둔다. 기둥이 있으면 글자로 읽히고,
    # 없으면 계급장 줄무늬로 읽힌다 — 이 한 끗이 '박혀있음'과 '문양'을 가른다.
    crest = "d" if step >= 3 else "G"   # 3단계부터 채도를 낮춰 바이저에 자리를 내준다
    row(g, 31, 14, 18, crest)
    row(g, 29, 14, 17, crest)
    row(g, 27, 14, 18, crest)

    # --- 허리띠 -------------------------------------------------------------
    rect(g, 10, 22, 20, 23, "W")
    rect(g, 14, 22, 17, 23, crest)
    if step < 3:
        row(g, 22, 15, 16, "g")

    # --- 팔 (양옆, 빈손) ------------------------------------------------------
    # 무기를 그리지 않는다 — 계속 갈아끼우는 게 이 게임의 핵심 루프다.
    rect(g, 7, 24, 10, 33, "B")
    rect(g, 20 + lean, 24, 23 + lean, 33, "B")
    rect(g, 22 + lean, 25, 23 + lean, 33, "b")

    for y in (28, 26, 24):           # 건틀릿의 세 줄
        row(g, y, 7, 10, "A")
        row(g, y, 20 + lean, 23 + lean, "A")

    # --- 다리 ---------------------------------------------------------------
    # 허벅지에서 종아리로 좁아진다. 폭이 일정하면 그 순간 로봇 다리가 된다.
    back_lift = 1 if step >= 4 else 0    # 무게를 앞다리에 싣고 뒷굽을 든다
    rect(g, 11, 14 + back_lift, 15, 21, "B")
    rect(g, 12, 5 + back_lift, 15, 13 + back_lift, "B")
    rect(g, 17, 14, 21, 21, "B")
    rect(g, 17, 5, 20, 13, "B")
    rect(g, 20, 15, 21, 20, "b")
    rect(g, 20, 6, 20, 12, "b")
    row(g, 14 + back_lift, 11, 15, "A")
    row(g, 13 + back_lift, 12, 15, "A")
    row(g, 14, 17, 21, "A")
    row(g, 13, 17, 20, "A")

    # --- 부츠 ---------------------------------------------------------------
    rect(g, 9, 0 + back_lift, 15, 5 + back_lift, "A")
    rect(g, 17, 0, 23, 5, "A")
    row(g, 5 + back_lift, 10, 15, "a")
    row(g, 5, 18, 23, "a")

    # --- 앞으로 쓸어 넘긴 망토 자락 (다리 앞) --------------------------------
    if step >= 1:
        # 뒤에서 늘어지기만 하면 좌우대칭이라 마네킹으로 보인다.
        # 자락 하나를 몸 앞으로 넘기면 그 순간 실루엣이 비대칭이 되고
        # '방금 움직였다'로 읽힌다 — 32px에서 가장 값싸고 강한 수단이다.
        # 처음엔 자락을 발치까지 늘였더니 부츠와 뭉쳐 검은 덩어리가 됐다.
        # 무릎 위에서 끝내 다리와 떨어뜨리고, 뒤집힌 안감은 '윗면'에 둔다 —
        # 천이 넘어오면 우리가 보는 건 안쪽 면이다.
        # 두 번 실패하고 알았다: 어두운 자락을 어두운 몸 **위에** 얹으면
        # 검은 부분이 안 보이고 밝은 안감만 남아 리본처럼 보인다.
        # 자락은 몸 밖 빈 공간으로 빼야 한다 — 그래야 실루엣 자체가 바뀐다.
        sweep = [
            (21, 21, 23), (20, 21, 25), (19, 21, 27), (18, 22, 28),
            (17, 22, 28), (16, 23, 27), (15, 24, 26), (14, 24, 25),
        ]
        for y, x0, x1 in sweep:
            row(g, y, x0, x1, "K")
        for y, x0, x1 in sweep:
            row(g, y, x0, x0, lining)   # 뒤집힌 안감은 안쪽 모서리에만

    return g


def add_outline(g):
    out = [r[:] for r in g]
    for y in range(H):
        for x in range(W):
            if g[y][x] != TRANSPARENT:
                continue
            found = False
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    if dx == 0 and dy == 0:
                        continue
                    ny, nx = y + dy, x + dx
                    if 0 <= ny < H and 0 <= nx < W and g[ny][nx] != TRANSPARENT:
                        found = True
                        break
                if found:
                    break
            if found:
                out[y][x] = OUTLINE
    return out


def hexes(**kw):
    return {k: (int(v[0:2], 16), int(v[2:4], 16), int(v[4:6], 16)) for k, v in kw.items()}


# 어둡되 뭉개지지 않게 — 명암 폭을 넓게 잡는다. 그게 '음침'과 '위압'을 가른다.
PALETTE = hexes(
    K="0A1119", k="1E3350",   # 망토 / 안감
    A="182634", a="3A506E",   # 갑주 / 빛 받는 면
    B="283848", b="3E6E9E",   # 몸통 / 앞면
    v="0A1119",               # 면갑 속 어둠
    E="00E5FF",               # 틈 셋
    p="101820",               # 문장 판
    G="FFCC00", g="FFF0A8", d="8C6A10",   # 금 / 밝은 금 / 죽인 금
    R="C22B2B", r="FF3B3B",   # 망토 안감 (유일한 보색)
    F="D8E4EE", n="1E3350",   # 바이저 중심 / 새어 나오는 빛
    W="3A2A1E",               # 가죽
)
PALETTE[OUTLINE] = (10, 17, 25)

SILHOUETTE = {k: (26, 32, 44) for k in PALETTE}
SILHOUETTE["E"] = (0, 229, 255)
SILHOUETTE[OUTLINE] = (10, 17, 25)


def to_rows(g, palette):
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


def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


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


def main():
    for step in range(5):
        g = add_outline(build(step))
        rows = to_rows(g, PALETTE)
        big, bw, bh = scale(rows, W, H, 8)
        write_png(os.path.join(PREVIEW_DIR, "knight_step%d_x8.png" % step), big, bw, bh)
        print("step%d" % step)

    # 최종본만 실제 스프라이트로 저장
    g = add_outline(build(4))
    rows = to_rows(g, PALETTE)
    write_png(os.path.join(SPRITE_DIR, "player_E_knight.png"), rows, W, H)

    big, bw, bh = scale(rows, W, H, 8)
    write_png(os.path.join(PREVIEW_DIR, "knight_x8.png"), big, bw, bh)

    small, sw, sh = shrink_half(rows, W, H)
    tiny, tw, th = scale(small, sw, sh, 8)
    write_png(os.path.join(PREVIEW_DIR, "knight_squint.png"), tiny, tw, th)

    sil, sw2, sh2 = scale(to_rows(g, SILHOUETTE), W, H, 8)
    write_png(os.path.join(PREVIEW_DIR, "knight_silhouette_x8.png"), sil, sw2, sh2)


if __name__ == "__main__":
    main()

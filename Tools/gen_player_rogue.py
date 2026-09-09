# 플레이어 캐릭터 — "세 줄의 도적".
#
# 앞선 두 시도가 실패한 이유:
#   1) E를 몸통으로 삼음 → 글자가 대놓고 박혀 보인다
#   2) E를 자세로 만듦   → 움직이는 순간 사라진다(플랫포머에서 치명적)
#
# 그래서 방향을 바꾼다: **E를 읽히게 하지 않고, 모티프로 반복한다.**
# E의 본질은 글자가 아니라 '평행한 세 줄, 가운데가 짧은'이다.
# 그 리듬을 캐릭터 곳곳에 심는다 —
#   후드 속 세 줄의 빛나는 틈 (얼굴 대신)
#   가슴을 가로지르는 가죽끈 세 줄
#   세 갈래로 찢어진 망토 자락
#   세 갈래 단검 (가운데 날이 짧다)
#
# 아무도 E를 알아보지 못해도 된다. 일관된 시각 언어가 생기고,
# "너는 알파벳 E다"를 아는 순간 전부 맞아떨어진다.
# 그리고 이 방식은 자세가 바뀌어도 유지되므로 애니메이션을 견딘다.
#
# 실행: python Tools/gen_player_rogue.py
# 결과: Assets/Sprites/player_E_rogue.png (32x48)
#       ArtSource/preview/rogue_x8.png, rogue_squint.png, rogue_silhouette_x8.png

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


def build():
    """앞으로 기울여 웅크린 도적.

    앞 판이 '보초병'으로 보였던 이유는 자세와 비율이었다 —
    똑바로 서고, 어깨가 넓고, 좌우대칭이고, 두 발을 벌려 딛고 있었다.
    도적은 그 반대다: 가늘고, 다리가 길고, 앞으로 기울고, 웅크리고, 비대칭이다.
    그리고 단검을 **거꾸로 쥔다** — 역수 그립은 암살자를 가장 짧게 설명하는 기호다.

    세로 배분 (48px):
      0-3  발    4-18 다리(15칸 — 길어야 날렵해 보인다)
      19-22 골반  23-37 몸통    38-47 후드

    세 줄 모티프 다섯 군데: 마스크 / 가슴끈 / 망토의 찢긴 자국 / 정강이 감개 / 칼등의 홈.
    이 중 가슴끈과 망토 자국만 가운데를 짧게 둔다 — E의 서명.
    """
    g = blank()

    # --- 망토 (가장 뒤) -----------------------------------------------------
    rect(g, 1, 12, 8, 36, "K")
    row(g, 11, 1, 7, "K")
    row(g, 10, 2, 6, "K")
    row(g, 9, 2, 4, "K")           # 뒤로 갈수록 처지는 비스듬한 단

    # 망토를 가로지르는 찢긴 자국 셋. 가운데가 짧다 — E의 서명 하나.
    # 낡은 천에 난 자국이라 억지스러운 데가 없다.
    row(g, 28, 2, 7, "b")
    row(g, 24, 2, 5, "b")
    row(g, 20, 2, 7, "b")

    # --- 후드 (앞으로 늘어져 얼굴을 삼킨다) ----------------------------------
    hood = [
        (47, 14, 18), (46, 13, 20), (45, 12, 22), (44, 11, 23),
        (43, 11, 23), (42, 11, 23), (41, 12, 23), (40, 12, 22),
        (39, 13, 21), (38, 13, 20),
    ]
    for y, x0, x1 in hood:
        row(g, y, x0, x1, "H")
    rect(g, 8, 34, 12, 42, "H")    # 뒤로 흘러 망토로 이어지는 자락

    row(g, 46, 14, 18, "h")        # 윗면이 빛을 받는다
    row(g, 45, 13, 17, "h")
    row(g, 44, 12, 15, "h")

    # --- 마스크 -------------------------------------------------------------
    void = [(43, 16, 22), (42, 15, 23), (41, 15, 23), (40, 15, 23),
            (39, 15, 22), (38, 16, 21), (37, 17, 20)]
    for y, x0, x1 in void:
        row(g, y, x0, x1, "v")

    # 길이를 맞춘다 — 얼굴에 글자를 박지 않기 위해. 여기선 '셋'이라는 리듬만 남긴다.
    row(g, 42, 17, 22, "E")
    row(g, 40, 17, 22, "E")
    row(g, 38, 17, 20, "E")

    # --- 몸통 (가늘고, 앞으로 기운다) ----------------------------------------
    # 위가 앞(오른쪽), 골반이 뒤(왼쪽). 폭 7~8칸 — 넓으면 그 순간 병사가 된다.
    torso = [
        (37, 11, 18), (36, 10, 18), (35, 10, 18), (34, 10, 17),
        (33, 10, 17), (32, 9, 17), (31, 9, 16), (30, 9, 16),
        (29, 9, 16), (28, 8, 15), (27, 8, 15), (26, 8, 15),
        (25, 8, 14), (24, 8, 14), (23, 8, 14),
    ]
    for y, x0, x1 in torso:
        row(g, y, x0, x1, "B")

    rect(g, 17, 30, 18, 36, "b")   # 앞면 하이라이트
    rect(g, 15, 24, 15, 28, "b")

    # --- 가슴끈 세 줄 -------------------------------------------------------
    # 가운데가 짧다 — E의 서명 둘. 밝은 색이라 어두운 던전에서 플레이어가 튄다.
    row(g, 34, 10, 17, "T")
    row(g, 31, 9, 13, "T")
    row(g, 28, 8, 15, "T")

    # --- 골반 / 허리띠 ------------------------------------------------------
    rect(g, 8, 19, 16, 22, "B")
    rect(g, 8, 20, 15, 21, "W")
    rect(g, 11, 20, 12, 21, "G")

    # --- 다리 (길게, 웅크려서, 비대칭) ---------------------------------------
    rect(g, 12, 13, 17, 19, "B")   # 앞 허벅지
    rect(g, 14, 4, 18, 14, "B")    # 앞 정강이
    rect(g, 18, 5, 18, 13, "b")
    rect(g, 15, 0, 21, 4, "W")     # 앞 부츠

    rect(g, 7, 13, 12, 19, "B")    # 뒤 허벅지
    rect(g, 5, 4, 9, 14, "B")      # 뒤 정강이
    rect(g, 2, 0, 9, 3, "W")       # 뒤 부츠

    # 정강이 감개 — 세 줄 (길이는 그대로)
    row(g, 11, 14, 18, "W")
    row(g, 9, 14, 18, "W")
    row(g, 7, 14, 18, "W")

    # --- 뒷팔 (뒤로 늘어뜨린 빈손) -------------------------------------------
    rect(g, 7, 26, 9, 33, "B")
    rect(g, 6, 23, 9, 26, "W")

    # --- 앞팔 + 역수로 쥔 단검 -----------------------------------------------
    # 칼날이 팔뚝을 따라 아래로 흐른다. 몸에서 떨어뜨려야 칼로 읽힌다 —
    # 앞 판은 날이 몸통과 겹쳐서 주머니처럼 보였다.
    # 팔은 반드시 몸통과 겹쳐서 시작한다. 한 칸이라도 뜨면 그 틈이 아웃라인으로 메워져
    # 캐릭터 한가운데에 구멍이 뚫린 것처럼 보인다.
    rect(g, 15, 26, 19, 35, "B")   # 위팔
    rect(g, 18, 23, 23, 26, "W")   # 아래팔 + 주먹 + 손잡이
    rect(g, 19, 22, 24, 22, "W")   # 코등이

    rect(g, 20, 13, 22, 21, "S")   # 날
    rect(g, 22, 13, 22, 21, "s")   # 날 등이 빛을 받는다
    rect(g, 21, 11, 21, 12, "S")   # 끝
    for y in (19, 17, 15):
        row(g, y, 20, 20, "W")     # 칼등의 홈 셋
    row(g, 12, 22, 22, "G")

    return g


def _unused_old_build():
    g = blank()

    # --- 망토 (가장 뒤) -----------------------------------------------------
    # 몸보다 어둡게 둬서 실루엣이 겹쳐 보이지 않게 한다.
    rect(g, 4, 14, 10, 42, "K")
    rect(g, 3, 20, 3, 36, "K")

    # 자락을 세 갈래로 찢어 봤지만 32px에서는 아웃라인이 틈을 메워 안 읽혔다.
    # 모티프는 마스크·가슴끈·정강이·칼등 네 군데로 충분하니, 여기는 너덜한 단으로만 둔다.
    row(g, 13, 3, 8, "K")
    row(g, 12, 3, 6, "K")
    row(g, 11, 4, 5, "K")
    row(g, 12, 9, 10, "K")
    row(g, 11, 9, 9, "K")

    # --- 후드 ---------------------------------------------------------------
    # 위가 평평하고 옆이 수직이면 투구가 된다.
    # 봉우리를 뒤(왼쪽)로 눕히고 앞으로 갈수록 낮아지게 해야 '천을 뒤집어썼다'로 읽힌다.
    hood = [
        (47, 8, 11), (46, 7, 14), (45, 6, 17), (44, 6, 19),
        (43, 5, 21), (42, 5, 22), (41, 5, 22), (40, 6, 21),
        (39, 7, 20), (38, 8, 19),
    ]
    for y, x0, x1 in hood:
        row(g, y, x0, x1, "H")
    rect(g, 5, 40, 8, 44, "H")     # 뒤로 젖혀진 후드 자락

    # 후드 윗면이 빛을 받는다 — 없으면 검은 덩어리가 된다
    row(g, 45, 7, 14, "h")
    row(g, 44, 7, 11, "h")
    row(g, 46, 8, 12, "h")

    # --- 마스크 -------------------------------------------------------------
    # 얼굴을 그리지 않는다. 후드 안쪽은 완전한 어둠이고,
    # 그 속에서 가로로 난 틈 세 개만 빛난다 — 모티프 2 (가장 눈길이 가는 자리).
    void = [(44, 15, 20), (43, 14, 21), (42, 14, 21),
            (41, 14, 21), (40, 14, 20), (39, 15, 19)]
    for y, x0, x1 in void:
        row(g, y, x0, x1, "v")

    # 길이를 똑같이 둔다. 가운데만 짧게 하면 그 순간 얼굴에 글자를 박은 게 된다 —
    # 여기서 노리는 건 '읽히는 E'가 아니라 '셋이라는 리듬'이다.
    row(g, 43, 16, 20, "E")
    row(g, 41, 16, 20, "E")
    row(g, 39, 16, 19, "E")

    # --- 몸통 ---------------------------------------------------------------
    # 직사각형이면 사람으로 안 보인다. 위로 갈수록 앞(오른쪽)으로 기울이고 허리를 좁힌다.
    torso = [
        (37, 10, 20), (36, 10, 21), (35, 10, 21), (34, 10, 20),
        (33, 10, 20), (32, 10, 19), (31, 10, 19), (30, 10, 19),
        (29, 11, 18), (28, 11, 18), (27, 11, 18), (26, 11, 17),
        (25, 11, 17), (24, 11, 17),
    ]
    for y, x0, x1 in torso:
        row(g, y, x0, x1, "B")

    # 어깨/가슴 앞면 하이라이트
    rect(g, 19, 30, 20, 36, "b")
    rect(g, 10, 30, 10, 36, "b")

    # --- 가슴끈 세 줄 -------------------------------------------------------
    # 가운데가 짧다 — 모티프 3. 밝은 색이라 어두운 던전에서 플레이어가 튄다.
    row(g, 34, 10, 20, "T")
    row(g, 31, 12, 17, "T")
    row(g, 28, 11, 18, "T")

    # --- 허리띠 -------------------------------------------------------------
    rect(g, 10, 22, 18, 23, "W")
    rect(g, 14, 22, 15, 23, "G")   # 버클

    # --- 다리 ---------------------------------------------------------------
    rect(g, 10, 17, 18, 21, "B")   # 골반
    rect(g, 9, 5, 12, 16, "B")     # 뒷다리
    rect(g, 16, 4, 20, 16, "B")    # 앞다리 (조금 앞으로)
    rect(g, 16, 4, 16, 16, "b")    # 앞다리 앞면 하이라이트

    # 정강이 감개 — 여기도 셋
    row(g, 12, 16, 20, "W")
    row(g, 10, 17, 19, "W")
    row(g, 8, 16, 20, "W")

    # --- 부츠 ---------------------------------------------------------------
    rect(g, 8, 0, 13, 4, "W")
    rect(g, 15, 0, 21, 3, "W")

    # --- 앞팔 + 세 갈래 단검 -------------------------------------------------
    rect(g, 18, 26, 21, 30, "B")   # 앞으로 내민 팔
    rect(g, 20, 25, 22, 27, "W")   # 손 + 손잡이
    rect(g, 23, 23, 23, 29, "W")   # 코등이

    # 세 갈래로 만들었더니 렌치로 보였다. 무기는 무엇보다 먼저 '칼'로 읽혀야 한다.
    # 모티프는 날 등에 판 홈 세 개로만 남긴다 — 알아보는 사람만 알아보면 된다.
    rect(g, 24, 25, 27, 27, "S")
    rect(g, 28, 25, 29, 26, "S")
    rect(g, 30, 26, 30, 26, "S")   # 끝
    row(g, 27, 24, 29, "s")        # 날 등이 빛을 받는다
    for x in (25, 27, 29):
        row(g, 25, x, x, "W")      # 등의 홈 셋
    row(g, 26, 30, 30, "G")

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


PALETTE = hexes(
    K="101820",   # 망토 (가장 어둡다)
    H="182634", h="283848",   # 후드 / 후드 윗면
    v="0A1119",   # 마스크 속 어둠
    E="00E5FF",   # 세 줄의 틈
    B="283848", b="3A506E",   # 몸통 / 앞면
    T="BA8C58",   # 가슴끈 — 유일한 따뜻한 색
    W="3A2A1E",   # 가죽 (허리띠·감개·부츠·손잡이)
    G="FFCC00",
    S="7FA0B8", s="D8E4EE",   # 강철 / 날 등
)
PALETTE[OUTLINE] = (10, 17, 25)

# 형태만 확인하는 실루엣 — 색이 판단을 흐리는 걸 막는다
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
    g = add_outline(build())

    rows = to_rows(g, PALETTE)
    write_png(os.path.join(SPRITE_DIR, "player_E_rogue.png"), rows, W, H)

    big, bw, bh = scale(rows, W, H, 8)
    write_png(os.path.join(PREVIEW_DIR, "rogue_x8.png"), big, bw, bh)

    small, sw, sh = shrink_half(rows, W, H)
    tiny, tw, th = scale(small, sw, sh, 8)
    write_png(os.path.join(PREVIEW_DIR, "rogue_squint.png"), tiny, tw, th)

    sil, sw2, sh2 = scale(to_rows(g, SILHOUETTE), W, H, 8)
    write_png(os.path.join(PREVIEW_DIR, "rogue_silhouette_x8.png"), sil, sw2, sh2)

    print("player_E_rogue  32x48")


if __name__ == "__main__":
    main()

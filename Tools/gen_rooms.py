# 방 템플릿(.txt) 생성기.
#
# 25x15 격자를 손으로 타이핑하면 한 줄만 길이가 어긋나도 파서가 거부한다.
# 빈 방에서 시작해 "스탬프"로 지형을 찍는 방식이라 길이가 항상 맞는다.
#
# 실행: python Tools/gen_rooms.py
# 생성물은 Assets/Rooms/*.txt — 이후 EditMode 테스트(RoomTemplateReachabilityTests)가 전량 검증한다.

import io
import os

# 설계 규칙 (Docs/LEVEL_DESIGN.md와 같은 수치):
#   - 발판은 아래 지면에서 3타일 이내 (점프 높이 3.51타일)
#     → 바닥(y=0) 위에 놓는 발판은 y=3 이하여야 한다(발 위치 y=4 = 단차 3)
#   - 수평 갭은 5칸 이하면 점프, 6~7칸은 대시 필요, 8칸 이상은 통과 불가
#   - 통로는 세로 2칸 이상 (웅크리기 없음)

SMALL = (25, 15)
WIDE = (51, 15)
TALL = (25, 31)

OUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Rooms")


def blank(size):
    """테두리 벽 + 바닥 + 4방향 문만 있는 빈 방."""
    w, h = size
    grid = [["." for _ in range(w)] for _ in range(h)]  # grid[y][x], y=0이 바닥
    for x in range(w):
        grid[0][x] = "="
        grid[h - 1][x] = "#"
    for y in range(h):
        grid[y][0] = "#"
        grid[y][w - 1] = "#"
    grid[0][0] = "="
    grid[0][w - 1] = "="
    # 문: 좌우는 바닥 바로 위, 상하는 가운데
    grid[1][0] = "D"
    grid[1][w - 1] = "D"
    grid[h - 1][w // 2] = "D"
    grid[0][w // 2] = "D"
    return grid


def stamp(grid, y, x, text):
    """(x, y)에서 오른쪽으로 text를 찍는다. y는 바닥이 0."""
    for i, c in enumerate(text):
        grid[y][x + i] = c


def put(grid, y, x, c):
    grid[y][x] = c


def render(grid):
    """텍스트는 위에서 아래로 — y가 큰 줄이 먼저."""
    return [("".join(row)) for row in reversed(grid)]


def write(name, grid, comments):
    lines = [";  " + c for c in comments]
    lines += render(grid)
    path = os.path.join(OUT_DIR, name + ".txt")
    io.open(path, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    widths = set(len(r) for r in render(grid))
    assert len(widths) == 1, (name, widths)
    print("%-32s %dx%d" % (name, list(widths)[0], len(render(grid))))


# --------------------------------------------------------------------------
# Tutorial — 첫 방. 지형은 평탄하게, 위험 요소 없음.
# --------------------------------------------------------------------------
def tutorial_01():
    g = blank(SMALL)
    put(g, 1, 4, "p")     # 플레이어 시작
    put(g, 1, 9, "l")     # 재료 픽업
    put(g, 1, 16, "x")    # 잠긴 문(Unlock 능력) — 출구 앞을 막는다
    stamp(g, 3, 19, "---")
    put(g, 4, 20, "l")
    return g, [
        "Tutorial_Small_01 — 첫 방(안전). 줍기 → 제작 → 장착 → 문 열기 수직 슬라이스.",
        "지형은 평탄하고 가시/구덩이 없음. x = 잠긴 문(Unlock).",
    ]


# --------------------------------------------------------------------------
# Combat — 근접 적은 바닥에, 원거리 적은 고지대에.
# 근접 적(수평 추격만 가능)이 못 오는 발판 위가 '무적 지대'가 되지 않도록
# 발판은 바닥과 3타일 이하로 이어 둔다.
# --------------------------------------------------------------------------
def combat_01():
    g = blank(SMALL)
    stamp(g, 3, 4, "-----")
    stamp(g, 3, 16, "-----")
    stamp(g, 6, 10, "-----")
    put(g, 1, 6, "e")
    put(g, 1, 18, "e")
    put(g, 4, 18, "E")       # 궁수를 고지대에
    put(g, 7, 12, "l")
    stamp(g, 5, 7, "??")
    stamp(g, 5, 16, "??")
    return g, [
        "Combat_Small_01 — 3단 발판. 궁수가 위에서 사선을 준다.",
        "발판 높이차 3타일 이하 — 근접 적이 못 오르는 안전지대를 만들지 않는다.",
    ]


def combat_02():
    g = blank(SMALL)
    # 계단식 지형 (오른쪽으로 올라간다)
    for i, (y, x0, x1) in enumerate([(1, 16, 23), (2, 18, 23), (3, 20, 23)]):
        for x in range(x0, x1 + 1):
            put(g, y, x, "=")
    stamp(g, 3, 6, "----")
    put(g, 1, 5, "e")
    put(g, 1, 11, "e")
    put(g, 4, 21, "E")
    put(g, 4, 7, "l")
    stamp(g, 6, 11, "???")
    return g, [
        "Combat_Small_02 — 오른쪽이 계단식 고지대. 왼쪽은 발판 하나.",
        "높이차를 이용해 궁수 사선과 근접 접근선을 분리한다.",
    ]


def combat_03():
    g = blank(SMALL)
    # 구덩이 4칸 — 남쪽 문(x=12)을 피해 오른쪽에 둔다
    for x in range(15, 19):
        put(g, 0, x, "~")
    stamp(g, 3, 9, "-----")
    put(g, 1, 5, "e")
    put(g, 1, 21, "e")
    put(g, 4, 11, "E")
    put(g, 1, 8, "l")
    return g, [
        "Combat_Small_03 — 가운데 구덩이가 전장을 둘로 나눈다.",
        "구덩이 폭 4칸 = 점프로 건너는 범위(물리 한계 5칸)이므로 대시 없이도 통과 가능.",
    ]


# --------------------------------------------------------------------------
# 퍼즐 — x 마커(CapabilityTarget)가 경로를 막는다.
# --------------------------------------------------------------------------
def env_puzzle_01():
    g = blank(SMALL)
    # 오른쪽 절반을 벽으로 막고, 뚫는 지점 하나만 x로 둔다
    for y in range(1, 9):
        put(g, y, 14, "#")
    put(g, 1, 14, "x")
    put(g, 2, 14, "x")
    stamp(g, 3, 4, "-----")
    put(g, 1, 6, "l")
    put(g, 1, 19, "l")
    put(g, 4, 6, "e")
    return g, [
        "EnvironmentPuzzle_Small_01 — 벽이 방을 둘로 가른다. x(부서지는 벽)를 뚫어야 오른쪽으로 간다.",
        "출구(동쪽 문)가 벽 너머라 능력 없이는 못 지나간다 — 레이어2 게이팅.",
    ]


def pure_puzzle_01():
    g = blank(SMALL)
    # 위로 올라가는 발판 사다리 — 각 단 3타일 이하
    stamp(g, 3, 3, "----")
    stamp(g, 6, 8, "----")
    stamp(g, 9, 13, "----")
    stamp(g, 11, 18, "----")
    put(g, 12, 20, "l")
    put(g, 4, 5, "x")
    put(g, 7, 10, "x")
    return g, [
        "PurePuzzle_Small_01 — 전투 없이 발판만 타고 올라가는 방.",
        "각 단의 높이차 3타일 이하(점프 한계 3.51타일) — 대시 없이 오를 수 있어야 한다.",
    ]


def combat_puzzle_01():
    g = blank(SMALL)
    stamp(g, 3, 5, "------")
    stamp(g, 3, 15, "-----")
    stamp(g, 6, 10, "----")
    put(g, 1, 4, "e")
    put(g, 1, 12, "e")
    put(g, 1, 20, "e")
    put(g, 4, 17, "E")
    put(g, 7, 11, "x")
    put(g, 1, 8, "l")
    return g, [
        "CombatPuzzle_Small_01 — 적 전멸 + 장애물 해제를 둘 다 해야 출구가 열린다.",
        "RoomPuzzle이 출구를 잠그므로 방 안에 완전한 안전지대를 두지 않는다.",
    ]


# --------------------------------------------------------------------------
# 보상 방
# --------------------------------------------------------------------------
def treasure_01():
    g = blank(SMALL)
    stamp(g, 3, 10, "-----")
    put(g, 4, 12, "c")     # 상자
    put(g, 1, 6, "l")
    put(g, 1, 18, "l")
    stamp(g, 2, 6, "??")
    stamp(g, 2, 17, "??")
    return g, [
        "Treasure_Small_01 — 보상 발판. 전투 없음.",
        "상자를 조금 높은 곳에 둬 '올라가서 얻는' 감각을 준다(3타일).",
    ]


def secret_01():
    g = blank(SMALL)
    # 바닥은 끊지 않는다 — 통행은 언제나 보장하고, 보상만 건너뛰기로 잠근다.
    stamp(g, 3, 4, "-----")
    stamp(g, 3, 15, "-----")   # 사이 6칸 공백 = 대시가 있어야 건넌다(물리 한계 7칸)
    put(g, 4, 17, "c")
    put(g, 1, 8, "l")
    put(g, 4, 5, "l")
    return g, [
        "Secret_Small_01 — 특수방. 공중 섬 사이 6칸을 건너야 상자에 닿는다(대시 필요).",
        "바닥은 이어져 있어 못 건너도 방은 통과 가능 — 보상은 선택, 통행은 보장.",
    ]


# --------------------------------------------------------------------------
# 보스 — Wide(51x15). 회피 공간을 넓게, 발판은 최소한만.
# --------------------------------------------------------------------------
def boss_01():
    g = blank(WIDE)
    stamp(g, 3, 6, "-----")
    stamp(g, 3, 40, "-----")
    stamp(g, 3, 23, "-----")
    put(g, 1, 25, "b")
    put(g, 1, 10, "l")
    put(g, 1, 40, "l")
    return g, [
        "Boss_Wide_01 — 51x15. 보스 사거리(2.2)와 aggro(9)를 고려해 도망칠 공간을 넓게 둔다.",
        "발판은 좌우 대칭 2개 + 중앙 1개, 전부 바닥에서 3타일 — 공중 무한 회피가 되지 않게.",
    ]


def main():
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)

    builders = [
        ("Tutorial_Small_01", tutorial_01),
        ("Combat_Small_01", combat_01),
        ("Combat_Small_02", combat_02),
        ("Combat_Small_03", combat_03),
        ("EnvironmentPuzzle_Small_01", env_puzzle_01),
        ("PurePuzzle_Small_01", pure_puzzle_01),
        ("CombatPuzzle_Small_01", combat_puzzle_01),
        ("Treasure_Small_01", treasure_01),
        ("Secret_Small_01", secret_01),
        ("Boss_Wide_01", boss_01),
    ]
    for name, fn in builders:
        grid, comments = fn()
        write(name, grid, comments)


if __name__ == "__main__":
    main()

# 프로젝트 팔레트(ArtSource/palette.png) 생성기.
#
# AI 아트 후처리(PixelArtPipeline)가 이 색 띠를 읽어 모든 스프라이트의 색을 여기로 몰아넣는다.
# 그래서 이 파일 하나가 게임 전체의 색조를 결정한다 — 톤을 바꾸고 싶으면 여기만 고치면 된다.
#
# 색조는 UI(Assets/Scripts/UI/UITheme.cs)의 레트로 픽셀 아케이드 톤에서 파생했다.
# UI와 게임 화면의 색이 따로 놀면 한 게임처럼 보이지 않는다.
#
# 실행: python Tools/gen_palette.py
# 외부 라이브러리 없이 PNG를 직접 쓴다(PIL 불필요).

import io
import os
import struct
import zlib

PALETTE = [
    # 어두운 중성 — 배경, 그림자, 아웃라인
    "0A1119", "101820", "182634", "283848", "3A506E",
    # 청색 / 시안 — UI 액센트2 계열, 금속, 얼음
    "1E3350", "3E6E9E", "00E5FF", "7FA0B8",
    # 밝은 중성 — 하이라이트, 글자
    "FFFFFF", "D8E4EE", "9AB0C4",
    # 금색 / 노랑 — UI 액센트, 열쇠·보상
    "FFF0A8", "FFCC00", "D9A100", "8C6A10",
    # 적색 — 위험, 가시, 체력
    "FF8A7A", "FF3B3B", "C22B2B", "7A1A1A",
    # 녹색 — 자연, 독, 회복
    "C8F0A0", "7FD860", "4E8C3C", "2E4A2E",
    # 갈색 / 흙 — 바닥, 나무 발판
    "BA8C58", "966A3E", "6A4A2E", "3A2A1E",
    # 보라 — 마법, 에테르, 특수방
    "E0B8FF", "A87ACC", "6A4A8C", "3A2A4E",
]

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "ArtSource", "palette.png")


def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def write_png(path, colors):
    width, height = len(colors), 1

    raw = b"\x00"  # 필터 타입 0 (None)
    for hexcode in colors:
        r = int(hexcode[0:2], 16)
        g = int(hexcode[2:4], 16)
        b = int(hexcode[4:6], 16)
        raw += bytes((r, g, b, 255))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))  # 8bit RGBA
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")

    directory = os.path.dirname(path)
    if not os.path.isdir(directory):
        os.makedirs(directory)
    io.open(path, "wb").write(png)


if __name__ == "__main__":
    assert len(PALETTE) == len(set(PALETTE)), "중복된 색이 있습니다"
    write_png(OUT, PALETTE)
    print("%s (%d colors)" % (OUT, len(PALETTE)))

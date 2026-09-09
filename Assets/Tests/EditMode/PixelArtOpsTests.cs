using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Help.EditorTools;

namespace Tests.EditMode
{
    // AI 아트 후처리의 순수 연산 검증.
    // 이 단계가 틀리면 스프라이트가 흐려지거나 격자에서 밀리는데, 눈으로는 "어딘가 이상하다"까지만 보인다.
    public class PixelArtOpsTests
    {
        private static Color32 C(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);

        // width x height 격자를 만든다. '.' = 마젠타 배경, '#' = 흰색, ' ' = 투명
        private static Color32[] Make(string[] rows)
        {
            var px = new Color32[rows[0].Length * rows.Length];
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[0].Length; x++)
                {
                    char ch = rows[y][x];
                    px[y * rows[0].Length + x] =
                        ch == '#' ? C(255, 255, 255)
                        : ch == ' ' ? PixelArtOps.Transparent
                        : PixelArtOps.MagentaKey;
                }
            return px;
        }

        [Test]
        public void KeyOutColorShouldClearBackgroundOnly()
        {
            var px = Make(new[] { "..#..", "..#.." });
            PixelArtOps.KeyOutColor(px, PixelArtOps.MagentaKey);

            Assert.AreEqual(0, px[0].a, "마젠타 배경은 투명해져야 한다");
            Assert.AreEqual(255, px[2].a, "그림은 남아야 한다");
        }

        [Test]
        public void KeyOutColorShouldToleratePartialMagenta()
        {
            // AI 출력은 배경색이 정확히 #FF00FF로 나오지 않는다
            var px = new[] { C(250, 6, 249), C(255, 255, 255) };
            PixelArtOps.KeyOutColor(px, PixelArtOps.MagentaKey);
            Assert.AreEqual(0, px[0].a);
            Assert.AreEqual(255, px[1].a);
        }

        [Test]
        public void FloodFillShouldSpareEnclosedBackgroundColor()
        {
            // 캐릭터 안쪽의 배경색(눈 같은 것)은 바깥과 이어져 있지 않으므로 살아야 한다
            var px = Make(new[]
            {
                ".....",
                ".###.",
                ".#.#.",
                ".###.",
                ".....",
            });
            PixelArtOps.FloodFillBackground(px, 5, 5);

            Assert.AreEqual(0, px[0].a, "바깥 배경은 지워진다");
            Assert.AreEqual(255, px[2 * 5 + 2].a, "테두리에 갇힌 같은 색은 남아야 한다");
        }

        [Test]
        public void TrimShouldCropToContent()
        {
            var px = Make(new[]
            {
                "     ",
                " ##  ",
                " ##  ",
                "     ",
            });
            var trimmed = PixelArtOps.Trim(px, 5, 4, out int w, out int h);
            Assert.AreEqual(2, w);
            Assert.AreEqual(2, h);
            Assert.AreEqual(4, trimmed.Length);
        }

        [Test]
        public void TrimShouldSurviveFullyTransparentImage()
        {
            var px = Make(new[] { "  ", "  " });
            PixelArtOps.Trim(px, 2, 2, out int w, out int h);
            Assert.AreEqual(2, w);
            Assert.AreEqual(2, h);
        }

        [Test]
        public void DownscaleShouldPickDominantColorNotAverage()
        {
            // 검정 3 + 흰색 1 → 평균(회색)이 아니라 최빈색(검정)이 나와야 한다
            var px = new[] { C(0, 0, 0), C(0, 0, 0), C(0, 0, 0), C(255, 255, 255) };
            var outPx = PixelArtOps.DownscaleMode(px, 2, 2, 1, 1);

            Assert.AreEqual(0, outPx[0].r, "평균을 내면 픽셀아트의 경계가 뭉개진다");
            Assert.AreEqual(0, outPx[0].b);
        }

        [Test]
        public void DownscaleShouldDropMostlyEmptyCells()
        {
            var px = new[] { PixelArtOps.Transparent, PixelArtOps.Transparent,
                             PixelArtOps.Transparent, C(255, 255, 255) };
            var outPx = PixelArtOps.DownscaleMode(px, 2, 2, 1, 1);
            Assert.AreEqual(0, outPx[0].a, "절반 이상 비어 있으면 실루엣이 번지지 않게 비운다");
        }

        [Test]
        public void DownscaleShouldProduceExactTargetSize()
        {
            var px = new Color32[64 * 96];
            for (int i = 0; i < px.Length; i++) px[i] = C(10, 20, 30);
            var outPx = PixelArtOps.DownscaleMode(px, 64, 96, 32, 48);
            Assert.AreEqual(32 * 48, outPx.Length);
        }

        [Test]
        public void QuantizeShouldSnapToNearestPaletteColor()
        {
            var palette = new List<Color32> { C(0, 0, 0), C(255, 255, 255), C(200, 30, 30) };
            var px = new[] { C(210, 40, 45), C(250, 250, 250) };
            PixelArtOps.QuantizeToPalette(px, palette);

            Assert.AreEqual(200, px[0].r);
            Assert.AreEqual(255, px[1].r);
        }

        [Test]
        public void QuantizeShouldPreserveAlpha()
        {
            var palette = new List<Color32> { C(0, 0, 0), C(255, 255, 255) };
            var px = new[] { C(120, 120, 120, 128) };
            PixelArtOps.QuantizeToPalette(px, palette);
            Assert.AreEqual(128, px[0].a);
        }

        [Test]
        public void QuantizeWithoutPaletteShouldBeNoOp()
        {
            var px = new[] { C(1, 2, 3) };
            PixelArtOps.QuantizeToPalette(px, null);
            Assert.AreEqual(2, px[0].g);
        }

        [Test]
        public void NearestShouldUsePerceptualDistance()
        {
            // 순수 RGB 거리로 고르면 밝기가 엉뚱하게 튀는 색이 뽑힌다.
            var palette = new List<Color32> { C(20, 20, 20), C(230, 230, 230) };
            Assert.AreEqual(230, PixelArtOps.Nearest(C(200, 205, 210), palette).r);
            Assert.AreEqual(20, PixelArtOps.Nearest(C(40, 35, 45), palette).r);
        }

        [Test]
        public void OutlineShouldGrowImageByOnePixelEachSide()
        {
            var px = new[] { C(255, 255, 255) };
            var outPx = PixelArtOps.AddOutline(px, 1, 1, C(0, 0, 0), out int w, out int h);
            Assert.AreEqual(3, w);
            Assert.AreEqual(3, h);
            Assert.AreEqual(255, outPx[1 * 3 + 1].r, "가운데 원본은 그대로");
            Assert.AreEqual(255, outPx[0].a, "모서리에도 테두리가 생긴다");
            Assert.AreEqual(0, outPx[0].r);
        }

        [Test]
        public void OutlineShouldNotOverwriteExistingPixels()
        {
            var px = new[] { C(255, 0, 0), C(0, 255, 0), C(0, 0, 255), C(255, 255, 0) };
            var outPx = PixelArtOps.AddOutline(px, 2, 2, C(0, 0, 0), out int w, out _);
            Assert.AreEqual(255, outPx[1 * w + 1].r);
            Assert.AreEqual(255, outPx[2 * w + 2].g);
        }
    }
}

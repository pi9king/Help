using System;
using System.Collections.Generic;
using UnityEngine;

namespace Help.EditorTools
{
    // AI가 만든 이미지를 게임에 쓸 픽셀아트로 바꾸는 순수 연산들.
    //
    // Unity 에디터 API를 쓰지 않아서 EditMode에서 그대로 테스트된다.
    // 파일 입출력·임포트는 PixelArtPipeline이 담당한다.
    public static class PixelArtOps
    {
        public static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        // AI에게 지시하기 가장 쉬운 배경색. 게임 팔레트에 절대 안 쓰는 색이라 오검출이 없다.
        public static readonly Color32 MagentaKey = new Color32(255, 0, 255, 255);

        // --- 배경 제거 ---------------------------------------------------------

        // 지정한 색과 가까운 픽셀을 투명하게. AI에게 "배경을 순수 마젠타로"라고 시키는 방식.
        public static void KeyOutColor(Color32[] px, Color32 key, int tolerance = 40)
        {
            for (int i = 0; i < px.Length; i++)
                if (px[i].a > 0 && Distance(px[i], key) <= tolerance) px[i] = Transparent;
        }

        // 네 모서리 색에서 시작해 번져 나가며 배경을 지운다.
        // 배경색을 지시하지 못한 이미지(미드저니 등)에 쓴다. 캐릭터 내부의 같은 색은 살아남는다.
        public static void FloodFillBackground(Color32[] px, int width, int height, int tolerance = 30)
        {
            var visited = new bool[px.Length];
            var queue = new Queue<int>();

            void Seed(int x, int y)
            {
                int i = y * width + x;
                if (!visited[i]) { visited[i] = true; queue.Enqueue(i); }
            }

            Seed(0, 0); Seed(width - 1, 0); Seed(0, height - 1); Seed(width - 1, height - 1);

            var seeds = new List<Color32>();
            foreach (var i in queue) seeds.Add(px[i]);

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                if (px[i].a == 0) continue;

                bool matches = false;
                foreach (var s in seeds) if (Distance(px[i], s) <= tolerance) { matches = true; break; }
                if (!matches) continue;

                px[i] = Transparent;

                int x = i % width, y = i / width;
                void Push(int nx, int ny)
                {
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
                    int n = ny * width + nx;
                    if (visited[n]) return;
                    visited[n] = true;
                    queue.Enqueue(n);
                }
                Push(x - 1, y); Push(x + 1, y); Push(x, y - 1); Push(x, y + 1);
            }
        }

        // --- 잘라내기 -----------------------------------------------------------

        // 알파가 있는 영역의 바운딩 박스로 자른다. 여백이 남으면 축소할 때 캐릭터가 작아진다.
        public static Color32[] Trim(Color32[] px, int width, int height, out int newWidth, out int newHeight)
        {
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (px[y * width + x].a > 0)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }

            if (maxX < 0) { newWidth = width; newHeight = height; return (Color32[])px.Clone(); }

            newWidth = maxX - minX + 1;
            newHeight = maxY - minY + 1;
            var outPx = new Color32[newWidth * newHeight];
            for (int y = 0; y < newHeight; y++)
                for (int x = 0; x < newWidth; x++)
                    outPx[y * newWidth + x] = px[(minY + y) * width + (minX + x)];
            return outPx;
        }

        // --- 축소 ---------------------------------------------------------------

        // 목표 격자로 줄인다. 평균(=bilinear)이 아니라 **최빈색**을 고른다 —
        // 평균을 내면 색이 섞여 픽셀아트 특유의 또렷한 경계가 사라진다.
        public static Color32[] DownscaleMode(Color32[] px, int width, int height, int targetW, int targetH)
        {
            var outPx = new Color32[targetW * targetH];
            var bucket = new Dictionary<int, int>();

            for (int ty = 0; ty < targetH; ty++)
                for (int tx = 0; tx < targetW; tx++)
                {
                    int x0 = tx * width / targetW, x1 = Mathf.Max(x0 + 1, (tx + 1) * width / targetW);
                    int y0 = ty * height / targetH, y1 = Mathf.Max(y0 + 1, (ty + 1) * height / targetH);

                    bucket.Clear();
                    int opaque = 0, total = 0;
                    for (int y = y0; y < y1 && y < height; y++)
                        for (int x = x0; x < x1 && x < width; x++)
                        {
                            total++;
                            var c = px[y * width + x];
                            if (c.a < 128) continue;
                            opaque++;
                            int key = (c.r << 16) | (c.g << 8) | c.b;
                            bucket.TryGetValue(key, out int n);
                            bucket[key] = n + 1;
                        }

                    // 절반 이상이 비어 있으면 이 칸은 비운다 — 실루엣이 번지지 않게.
                    if (total == 0 || opaque * 2 < total) { outPx[ty * targetW + tx] = Transparent; continue; }

                    int bestKey = 0, bestCount = -1;
                    foreach (var kv in bucket)
                        if (kv.Value > bestCount || (kv.Value == bestCount && kv.Key < bestKey))
                        { bestKey = kv.Key; bestCount = kv.Value; }

                    outPx[ty * targetW + tx] = new Color32(
                        (byte)((bestKey >> 16) & 0xFF), (byte)((bestKey >> 8) & 0xFF), (byte)(bestKey & 0xFF), 255);
                }

            return outPx;
        }

        // --- 팔레트 -------------------------------------------------------------

        // 프로젝트 팔레트로 색을 몰아넣는다. 이게 여러 에셋을 "같은 게임의 그림"으로 만든다.
        // 디더링은 하지 않는다 — 픽셀아트에서는 노이즈로만 보인다.
        public static void QuantizeToPalette(Color32[] px, IReadOnlyList<Color32> palette)
        {
            if (palette == null || palette.Count == 0) return;

            var cache = new Dictionary<int, Color32>();
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a == 0) continue;
                int key = (px[i].r << 16) | (px[i].g << 8) | px[i].b;
                if (!cache.TryGetValue(key, out var mapped))
                {
                    mapped = Nearest(px[i], palette);
                    cache[key] = mapped;
                }
                px[i] = new Color32(mapped.r, mapped.g, mapped.b, px[i].a);
            }
        }

        // 지각적으로 가장 가까운 색. RGB 거리로 고르면 밝기가 튀는 색이 뽑힌다.
        public static Color32 Nearest(Color32 c, IReadOnlyList<Color32> palette)
        {
            var target = ToOklab(c);
            Color32 best = palette[0];
            float bestD = float.MaxValue;
            foreach (var p in palette)
            {
                var lab = ToOklab(p);
                float d = (lab.x - target.x) * (lab.x - target.x)
                        + (lab.y - target.y) * (lab.y - target.y)
                        + (lab.z - target.z) * (lab.z - target.z);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        // --- 아웃라인 -----------------------------------------------------------

        // 실루엣 바깥에 1픽셀 테두리. 배경 위에서 캐릭터가 묻히지 않게 한다.
        public static Color32[] AddOutline(Color32[] px, int width, int height, Color32 color,
                                           out int newWidth, out int newHeight)
        {
            newWidth = width + 2;
            newHeight = height + 2;
            var outPx = new Color32[newWidth * newHeight];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    outPx[(y + 1) * newWidth + (x + 1)] = px[y * width + x];

            var result = (Color32[])outPx.Clone();
            for (int y = 0; y < newHeight; y++)
                for (int x = 0; x < newWidth; x++)
                {
                    if (outPx[y * newWidth + x].a > 0) continue;
                    if (!HasOpaqueNeighbor(outPx, newWidth, newHeight, x, y)) continue;
                    result[y * newWidth + x] = color;
                }
            return result;
        }

        private static bool HasOpaqueNeighbor(Color32[] px, int w, int h, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (px[ny * w + nx].a > 0) return true;
                }
            return false;
        }

        // --- 색 공간 ------------------------------------------------------------

        public static int Distance(Color32 a, Color32 b)
        {
            int dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return (int)Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        // sRGB → OkLab. 사람 눈이 느끼는 색 차이에 훨씬 가깝다.
        public static Vector3 ToOklab(Color32 c)
        {
            float r = SrgbToLinear(c.r / 255f);
            float g = SrgbToLinear(c.g / 255f);
            float b = SrgbToLinear(c.b / 255f);

            float l = 0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b;
            float m = 0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b;
            float s = 0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b;

            float l_ = Cbrt(l), m_ = Cbrt(m), s_ = Cbrt(s);

            return new Vector3(
                0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
                1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
                0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_);
        }

        private static float SrgbToLinear(float v) =>
            v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);

        private static float Cbrt(float v) => v <= 0f ? 0f : Mathf.Pow(v, 1f / 3f);
    }
}

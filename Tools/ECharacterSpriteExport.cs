using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class ECharacterSpriteExport
{
    private struct Bounds
    {
        public int Left, Top, Right, Bottom;
        public bool HasPixels { get { return Right >= Left && Bottom >= Top; } }
        public int Width { get { return Right - Left + 1; } }
        public int Height { get { return Bottom - Top + 1; } }
    }

    private struct Cell
    {
        public int X, Y, Width, Height;
        public Bounds Sprite;
    }

    private static bool IsBackground(Color c)
    {
        return c.R >= 185 && c.B >= 165 && c.G <= 125 && Math.Abs(c.R - c.B) <= 85;
    }

    private static Cell GetCell(Bitmap source, int cols, int rows, int index)
    {
        int col = index % cols;
        int row = index / cols;
        int x0 = (int)Math.Round((double)col * source.Width / cols);
        int y0 = (int)Math.Round((double)row * source.Height / rows);
        int x1 = (int)Math.Round((double)(col + 1) * source.Width / cols);
        int y1 = (int)Math.Round((double)(row + 1) * source.Height / rows);
        return new Cell { X = x0, Y = y0, Width = x1 - x0, Height = y1 - y0 };
    }

    private static Bounds FindBounds(Bitmap source, Cell cell)
    {
        var result = new Bounds { Left = cell.Width, Top = cell.Height, Right = -1, Bottom = -1 };
        // The generated Skill sheet has a thin black grid. Do not include it.
        const int border = 5;
        for (int y = border; y < cell.Height - border; y++)
        for (int x = border; x < cell.Width - border; x++)
        {
            if (IsBackground(source.GetPixel(cell.X + x, cell.Y + y))) continue;
            result.Left = Math.Min(result.Left, x);
            result.Top = Math.Min(result.Top, y);
            result.Right = Math.Max(result.Right, x);
            result.Bottom = Math.Max(result.Bottom, y);
        }
        return result;
    }

    private static double FirstFramePivot(Bitmap source, Cell first)
    {
        Bounds b = first.Sprite;
        int footTop = b.Bottom - Math.Max(1, b.Height / 8);
        int left = first.Width, right = -1;
        for (int y = footTop; y <= b.Bottom; y++)
        for (int x = b.Left; x <= b.Right; x++)
        {
            if (IsBackground(source.GetPixel(first.X + x, first.Y + y))) continue;
            left = Math.Min(left, x);
            right = Math.Max(right, x);
        }
        return right >= left ? (left + right) / 2.0 : (b.Left + b.Right) / 2.0;
    }

    private static double FootCenter(Bitmap source, Cell cell)
    {
        Bounds b = cell.Sprite;
        int footTop = b.Bottom - Math.Max(2, b.Height / 8);
        double sum = 0;
        int count = 0;
        for (int y = footTop; y <= b.Bottom; y++)
        for (int x = b.Left; x <= b.Right; x++)
        {
            if (IsBackground(source.GetPixel(cell.X + x, cell.Y + y))) continue;
            sum += x;
            count++;
        }
        return count > 0 ? sum / count : (b.Left + b.Right) / 2.0;
    }

    public static string ExportFrames(string sourcePath, int cols, int rows, int count,
        string transparentDir, string magentaDir, bool keepCharacterHeight, bool fitTallEffects,
        bool alignPerFrame)
    {
        Directory.CreateDirectory(transparentDir);
        Directory.CreateDirectory(magentaDir);
        using (var source = new Bitmap(sourcePath))
        {
            var cells = new List<Cell>();
            for (int i = 0; i < count; i++)
            {
                Cell cell = GetCell(source, cols, rows, i);
                cell.Sprite = FindBounds(source, cell);
                if (!cell.Sprite.HasPixels)
                    throw new InvalidOperationException("Empty generated cell " + (i + 1) + " in " + sourcePath);
                cells.Add(cell);
            }

            double pivotX = FirstFramePivot(source, cells[0]);
            double groundY = int.MinValue;
            foreach (Cell cell in cells) groundY = Math.Max(groundY, cell.Sprite.Bottom);
            Bounds first = cells[0].Sprite;
            double scale = Math.Min(52.0 / first.Height, 62.0 / first.Width);
            double scaleX = scale;
            var framePivots = new double[count];
            for (int i = 0; i < count; i++) framePivots[i] = pivotX;
            if (keepCharacterHeight)
            {
                int left = int.MaxValue, right = int.MinValue, top = int.MaxValue;
                foreach (Cell cell in cells)
                {
                    left = Math.Min(left, cell.Sprite.Left);
                    right = Math.Max(right, cell.Sprite.Right);
                    top = Math.Min(top, cell.Sprite.Top);
                }
                pivotX = (left + right) / 2.0;
                scale = 52.0 / first.Height;
                if (fitTallEffects) scale = Math.Min(scale, 59.0 / (groundY - top + 1));
                scaleX = Math.Min(scale, 60.0 / (right - left + 1));
                if (alignPerFrame)
                {
                    double leftReach = 1, rightReach = 1;
                    int tallest = 1;
                    for (int i = 0; i < count; i++)
                    {
                        framePivots[i] = FootCenter(source, cells[i]);
                        leftReach = Math.Max(leftReach, framePivots[i] - cells[i].Sprite.Left);
                        rightReach = Math.Max(rightReach, cells[i].Sprite.Right - framePivots[i]);
                        tallest = Math.Max(tallest, cells[i].Sprite.Height);
                    }
                    scale = Math.Min(52.0 / first.Height, 59.0 / tallest);
                    scaleX = Math.Min(scale, Math.Min(30.0 / leftReach, 30.0 / rightReach));
                }
            }
            if (!keepCharacterHeight)
            {
                double left = 1, right = 1, above = 1;
                foreach (Cell cell in cells)
                {
                    left = Math.Max(left, pivotX - cell.Sprite.Left);
                    right = Math.Max(right, cell.Sprite.Right - pivotX);
                    above = Math.Max(above, groundY - cell.Sprite.Top);
                }
                scale = Math.Min(scale, 31.0 / left);
                scale = Math.Min(scale, 31.0 / right);
                scale = Math.Min(scale, 55.0 / above);
                scaleX = scale;
            }

            if (!alignPerFrame)
                for (int i = 0; i < count; i++) framePivots[i] = pivotX;

            for (int i = 0; i < count; i++)
            {
                Cell cell = cells[i];
                using (var transparent = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
                using (var magenta = new Bitmap(64, 64, PixelFormat.Format24bppRgb))
                {
                    for (int y = 0; y < 64; y++)
                    for (int x = 0; x < 64; x++)
                    {
                        double sourceX = framePivots[i] + (x + 0.5 - 32.0) / scaleX;
                        double sourceY = (alignPerFrame ? cell.Sprite.Bottom : groundY) +
                            (y + 0.5 - 61.0) / scale;
                        int sx = (int)Math.Round(sourceX);
                        int sy = (int)Math.Round(sourceY);
                        Color c = Color.Magenta;
                        if (sx >= 5 && sx < cell.Width - 5 && sy >= 5 && sy < cell.Height - 5)
                        {
                            Color sample = source.GetPixel(cell.X + sx, cell.Y + sy);
                            if (!IsBackground(sample)) c = sample;
                        }
                        magenta.SetPixel(x, y, c);
                        transparent.SetPixel(x, y, c.ToArgb() == Color.Magenta.ToArgb()
                            ? Color.Transparent : Color.FromArgb(255, c.R, c.G, c.B));
                    }
                    string file = "Frame_" + (i + 1).ToString("00") + ".png";
                    transparent.Save(Path.Combine(transparentDir, file), ImageFormat.Png);
                    magenta.Save(Path.Combine(magentaDir, file), ImageFormat.Png);
                }
            }
            return Path.GetFileName(sourcePath) + ": " + count + " frames, scale=" +
                scaleX.ToString("0.000") + "x" + scale.ToString("0.000");
        }
    }

    public static void Assemble(string[] framePaths, int cols, int rows, string outputPath,
        bool magentaBackground)
    {
        using (var sheet = new Bitmap(cols * 64, rows * 64, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(sheet))
        {
            graphics.Clear(magentaBackground ? Color.Magenta : Color.Transparent);
            for (int i = 0; i < framePaths.Length; i++)
            {
                using (var frame = Image.FromFile(framePaths[i]))
                    graphics.DrawImageUnscaled(frame, (i % cols) * 64, (i / cols) * 64);
            }
            sheet.Save(outputPath, ImageFormat.Png);
        }
    }

    public static void AnchorSkillFrame(string skillPath, string idlePath, string magentaPath)
    {
        using (var skill = new Bitmap(skillPath))
        using (var idle = new Bitmap(idlePath))
        using (var transparent = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
        using (var magenta = new Bitmap(64, 64, PixelFormat.Format24bppRgb))
        {
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color effect = skill.GetPixel(x, y);
                bool outsideBody = x < 12 || x > 52 || y >= 48;
                bool magicColor = effect.A > 0 && effect.B >= 135 && effect.G >= 85 &&
                    effect.B > effect.R * 1.25 && effect.G > effect.R * 1.15;
                Color c = outsideBody && magicColor ? effect : Color.Transparent;
                Color body = idle.GetPixel(x, y);
                if (body.A > 0) c = body;
                transparent.SetPixel(x, y, c);
                magenta.SetPixel(x, y, c.A > 0 ? Color.FromArgb(c.R, c.G, c.B) : Color.Magenta);
            }
            // Bitmap inputs must be disposed before overwriting the generated frame.
            var outTransparent = new Bitmap(transparent);
            var outMagenta = new Bitmap(magenta);
            skill.Dispose();
            idle.Dispose();
            try
            {
                outTransparent.Save(skillPath, ImageFormat.Png);
                outMagenta.Save(magentaPath, ImageFormat.Png);
            }
            finally
            {
                outTransparent.Dispose();
                outMagenta.Dispose();
            }
        }
    }

    public static void EnlargeNearest(string sourcePath, string outputPath, int factor)
    {
        using (var source = Image.FromFile(sourcePath))
        using (var output = new Bitmap(source.Width * factor, source.Height * factor))
        using (var graphics = Graphics.FromImage(output))
        {
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(source, 0, 0, output.Width, output.Height);
            output.Save(outputPath, ImageFormat.Png);
        }
    }
}

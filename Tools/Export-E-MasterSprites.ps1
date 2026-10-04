Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class EMasterSpriteExport
{
    private static bool IsMagenta(Color c)
    {
        return c.R >= 185 && c.B >= 165 && c.G <= 125 && Math.Abs(c.R - c.B) <= 85;
    }

    public static string Export(string sourcePath, string outputPath)
    {
        using (var source = new Bitmap(sourcePath))
        {
            int left = source.Width, top = source.Height, right = -1, bottom = -1;
            for (int y = 0; y < source.Height; y++)
            for (int x = 0; x < source.Width; x++)
            {
                if (IsMagenta(source.GetPixel(x, y))) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
            if (right < left) throw new InvalidOperationException("No sprite pixels: " + sourcePath);

            int footLeft = source.Width, footRight = -1;
            int footTop = bottom - Math.Max(1, (bottom - top + 1) / 8);
            for (int y = footTop; y <= bottom; y++)
            for (int x = left; x <= right; x++)
            {
                if (IsMagenta(source.GetPixel(x, y))) continue;
                footLeft = Math.Min(footLeft, x);
                footRight = Math.Max(footRight, x);
            }
            double pivot = footRight >= footLeft ? (footLeft + footRight) / 2.0 : (left + right) / 2.0;
            double width = right - left + 1;
            double height = bottom - top + 1;
            double scale = Math.Min(52.0 / height, 62.0 / width);
            scale = Math.Min(scale, 31.0 / Math.Max(1.0, pivot - left));
            scale = Math.Min(scale, 31.0 / Math.Max(1.0, right - pivot));
            double dstLeft = 32.0 - (pivot - left) * scale;
            double dstTop = 61.0 - height * scale;

            using (var output = new Bitmap(64, 64, PixelFormat.Format24bppRgb))
            {
                for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    double sx = left + (x + 0.5 - dstLeft) / scale;
                    double sy = top + (y + 0.5 - dstTop) / scale;
                    Color color = Color.Magenta;
                    if (sx >= left && sx <= right && sy >= top && sy <= bottom)
                    {
                        Color sample = source.GetPixel((int)Math.Round(sx), (int)Math.Round(sy));
                        if (!IsMagenta(sample)) color = sample;
                    }
                    output.SetPixel(x, y, color);
                }
                output.Save(outputPath, ImageFormat.Png);
            }
            return Path.GetFileName(outputPath) + ": " + Math.Round(width * scale) + "x" + Math.Round(height * scale) + "px occupied";
        }
    }
}
'@

$root = Join-Path $PSScriptRoot '..\ArtSource\E_Master'
$sourceDir = Join-Path $root 'AI_Source'
$outputDir = Join-Path $root '64px'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$directions = @('Down', 'Left', 'Right', 'Up')
foreach ($direction in $directions) {
    $source = Join-Path $sourceDir "E_Master_${direction}_AI.png"
    $output = Join-Path $outputDir "E_Master_${direction}.png"
    [EMasterSpriteExport]::Export($source, $output)
}

$sheet = New-Object System.Drawing.Bitmap(256, 64)
$graphics = [System.Drawing.Graphics]::FromImage($sheet)
try {
    for ($i = 0; $i -lt $directions.Count; $i++) {
        $path = Join-Path $outputDir "E_Master_$($directions[$i]).png"
        $sprite = [System.Drawing.Image]::FromFile($path)
        try { $graphics.DrawImageUnscaled($sprite, $i * 64, 0) }
        finally { $sprite.Dispose() }
    }
    $sheet.Save((Join-Path $outputDir 'E_Master_DirectionSheet.png'), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $graphics.Dispose()
    $sheet.Dispose()
}

$sheetPath = Join-Path $outputDir 'E_Master_DirectionSheet.png'
$previewPath = Join-Path $outputDir 'E_Master_DirectionSheet_x8.png'
$sourceSheet = [System.Drawing.Image]::FromFile($sheetPath)
$preview = New-Object System.Drawing.Bitmap(2048, 512)
$previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
try {
    $previewGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $previewGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $previewGraphics.DrawImage($sourceSheet, 0, 0, 2048, 512)
    $preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $previewGraphics.Dispose()
    $preview.Dispose()
    $sourceSheet.Dispose()
}

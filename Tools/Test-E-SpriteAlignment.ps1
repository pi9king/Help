$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$spriteDir = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..')).Path 'Assets\Sprites\E_Character'
$grids = [ordered]@{
    Master = @(4, 1)
    Idle = @(4, 4)
    Walk = @(6, 4)
    Attack = @(6, 4)
    Hit = @(3, 4)
    Death = @(8, 1)
    Skill = @(6, 1)
    Equipment = @(7, 1)
}
$total = 0

foreach ($name in $grids.Keys) {
    $grid = $grids[$name]
    $path = Join-Path $spriteDir "E_$name.png"
    $bitmap = [System.Drawing.Bitmap]::FromFile($path)
    try {
        if ($bitmap.Width -ne $grid[0] * 64 -or $bitmap.Height -ne $grid[1] * 64) {
            throw "Unexpected sheet dimensions: $name"
        }
        for ($row = 0; $row -lt $grid[1]; $row++) {
            for ($column = 0; $column -lt $grid[0]; $column++) {
                $bottom = -1
                $edgeCount = 0
                for ($y = 0; $y -lt 64; $y++) {
                    for ($x = 0; $x -lt 64; $x++) {
                        $alpha = $bitmap.GetPixel($column * 64 + $x, $row * 64 + $y).A
                        if ($alpha -gt 0 -and $alpha -lt 255) { throw "Partial alpha: $name row=$row column=$column" }
                        if ($alpha -eq 255) {
                            $bottom = [Math]::Max($bottom, $y)
                            if ($x -eq 0 -or $x -eq 63 -or $y -eq 0 -or $y -eq 63) { $edgeCount++ }
                        }
                    }
                }
                if ($bottom -lt 0) { throw "Empty sprite: $name row=$row column=$column" }

                if ($name -in @('Idle', 'Walk', 'Attack', 'Hit')) {
                    if ($bottom -ne 60 -or $edgeCount -gt 0) {
                        throw "Misaligned or clipped: $name row=$row column=$column bottom=$bottom edge=$edgeCount"
                    }
                    $sumX = 0
                    $pixelCount = 0
                    for ($y = $bottom - 6; $y -le $bottom; $y++) {
                        for ($x = 0; $x -lt 64; $x++) {
                            if ($bitmap.GetPixel($column * 64 + $x, $row * 64 + $y).A -eq 255) {
                                $sumX += $x
                                $pixelCount++
                            }
                        }
                    }
                    $footX = $sumX / $pixelCount
                    if ([Math]::Abs($footX - 32) -gt 2) {
                        throw "Horizontal foot drift: $name row=$row column=$column footX=$footX"
                    }
                }
                $total++
            }
        }
        Write-Output "E_$name.png OK"
    }
    finally { $bitmap.Dispose() }
}
Write-Output "Validated $total sprite cells."

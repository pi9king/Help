$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'ECharacterSpriteExport.cs') -ReferencedAssemblies System.Drawing

$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$art = Join-Path $project 'ArtSource\E_Animation'
$sourceDir = Join-Path $art 'AI_Source'
$frameDir = Join-Path $art 'Frames_Transparent'
$frameMagentaDir = Join-Path $art 'Frames_Magenta'
$magentaSheetDir = Join-Path $art '64px_Magenta'
$previewDir = Join-Path $art 'Preview'
$unityDir = Join-Path $project 'Assets\Sprites\E_Character'
@($frameDir, $frameMagentaDir, $magentaSheetDir, $previewDir, $unityDir) |
    ForEach-Object { New-Item -ItemType Directory -Path $_ -Force | Out-Null }

$directions = @('Down', 'Left', 'Right', 'Up')
$animations = @(
    @{ Name = 'Idle'; Count = 4; SourceCols = 2; SourceRows = 2; Fps = 7 },
    @{ Name = 'Walk'; Count = 6; SourceCols = 3; SourceRows = 2; Fps = 11 },
    @{ Name = 'Attack'; Count = 6; SourceCols = 3; SourceRows = 2; Fps = 12 },
    @{ Name = 'Hit'; Count = 3; SourceCols = 3; SourceRows = 1; Fps = 11 }
)

foreach ($animation in $animations) {
    $transparentPaths = [System.Collections.Generic.List[string]]::new()
    $magentaPaths = [System.Collections.Generic.List[string]]::new()
    foreach ($direction in $directions) {
        $stem = "$($animation.Name)_$direction"
        $source = Join-Path $sourceDir "$stem.png"
        $transparentFrames = Join-Path $frameDir $stem
        $magentaFrames = Join-Path $frameMagentaDir $stem
        [ECharacterSpriteExport]::ExportFrames($source, $animation.SourceCols,
            $animation.SourceRows, $animation.Count, $transparentFrames, $magentaFrames,
            $true, $true, $true)
        for ($i = 1; $i -le $animation.Count; $i++) {
            $file = 'Frame_{0:00}.png' -f $i
            $transparentPaths.Add((Join-Path $transparentFrames $file))
            $magentaPaths.Add((Join-Path $magentaFrames $file))
        }
    }
    $name = "E_$($animation.Name).png"
    [ECharacterSpriteExport]::Assemble($transparentPaths.ToArray(), $animation.Count, 4,
        (Join-Path $unityDir $name), $false)
    $magentaSheet = Join-Path $magentaSheetDir $name
    [ECharacterSpriteExport]::Assemble($magentaPaths.ToArray(), $animation.Count, 4,
        $magentaSheet, $true)
    [ECharacterSpriteExport]::EnlargeNearest($magentaSheet,
        (Join-Path $previewDir "E_$($animation.Name)_x4.png"), 4)
}

$singleAnimations = @(
    @{ Name = 'Death'; Count = 8; SourceCols = 4; SourceRows = 2; Fps = 9; KeepHeight = $false; FitTallEffects = $false },
    @{ Name = 'Skill'; Count = 6; SourceCols = 3; SourceRows = 2; Fps = 11; KeepHeight = $true; FitTallEffects = $false },
    @{ Name = 'Equipment'; Count = 7; SourceCols = 4; SourceRows = 2; Fps = 0; KeepHeight = $false; FitTallEffects = $false }
)
foreach ($animation in $singleAnimations) {
    $stem = $animation.Name
    $source = Join-Path $sourceDir "$stem.png"
    $transparentFrames = Join-Path $frameDir $stem
    $magentaFrames = Join-Path $frameMagentaDir $stem
    [ECharacterSpriteExport]::ExportFrames($source, $animation.SourceCols,
        $animation.SourceRows, $animation.Count, $transparentFrames, $magentaFrames,
        $animation.KeepHeight, $animation.FitTallEffects, $false)
    $transparentPaths = @()
    $magentaPaths = @()
    for ($i = 1; $i -le $animation.Count; $i++) {
        $file = 'Frame_{0:00}.png' -f $i
        if ($stem -eq 'Skill') {
            $idleFrame = 'Frame_{0:00}.png' -f ((($i - 1) % 4) + 1)
            [ECharacterSpriteExport]::AnchorSkillFrame(
                (Join-Path $transparentFrames $file),
                (Join-Path (Join-Path $frameDir 'Idle_Down') $idleFrame),
                (Join-Path $magentaFrames $file))
        }
        $transparentPaths += Join-Path $transparentFrames $file
        $magentaPaths += Join-Path $magentaFrames $file
    }
    $name = "E_$stem.png"
    [ECharacterSpriteExport]::Assemble($transparentPaths, $animation.Count, 1,
        (Join-Path $unityDir $name), $false)
    $magentaSheet = Join-Path $magentaSheetDir $name
    [ECharacterSpriteExport]::Assemble($magentaPaths, $animation.Count, 1,
        $magentaSheet, $true)
    [ECharacterSpriteExport]::EnlargeNearest($magentaSheet,
        (Join-Path $previewDir "E_${stem}_x4.png"), 4)
}

$masterMagenta = Join-Path $project 'ArtSource\E_Master\64px\E_Master_DirectionSheet.png'
$masterTransparent = Join-Path $unityDir 'E_Master.png'
$masterBitmap = [System.Drawing.Bitmap]::FromFile($masterMagenta)
try {
    $masterBitmap.MakeTransparent([System.Drawing.Color]::Magenta)
    $masterBitmap.Save($masterTransparent, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $masterBitmap.Dispose() }

$metadata = [ordered]@{
    cell_size = 64
    pivot = 'bottom-center (0.5, 0.0)'
    row_order = $directions
    animations = [ordered]@{
        Idle = @{ frames_per_direction = 4; rows = 4; fps = 7; loop = $true }
        Walk = @{ frames_per_direction = 6; rows = 4; fps = 11; loop = $true }
        Attack = @{ frames_per_direction = 6; rows = 4; fps = 12; loop = $false }
        Hit = @{ frames_per_direction = 3; rows = 4; fps = 11; loop = $false }
        Death = @{ frames = 8; rows = 1; direction = 'Down/common'; fps = 9; loop = $false }
        Skill = @{ frames = 6; rows = 1; direction = 'Down/common'; fps = 11; loop = $false }
        Equipment = @{ frames = 7; rows = 1; direction = 'Down'; fps = 0; order = @('Unarmed','Sword','Shield','Staff','Spellbook','Helmet','Cape Variation') }
    }
}
$metadata | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $art 'animation_manifest.json') -Encoding UTF8

param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$packRoot = Join-Path $ProjectRoot 'Assets\UI\Packs\PaperBook'
$sourceRoot = Join-Path $packRoot 'Sprites\Content'
$outputRoot = Join-Path $packRoot 'SpriteSheets\Icons'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$entries = @()
$empty = @()
foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.png' | Sort-Object FullName) {
    $image = [System.Drawing.Bitmap]::new($file.FullName)
    try {
        $minX = $image.Width; $minY = $image.Height; $maxX = -1; $maxY = -1
        for ($y = 0; $y -lt $image.Height; $y++) {
            for ($x = 0; $x -lt $image.Width; $x++) {
                if ($image.GetPixel($x, $y).A -gt 0) {
                    $minX = [Math]::Min($minX, $x); $maxX = [Math]::Max($maxX, $x)
                    $minY = [Math]::Min($minY, $y); $maxY = [Math]::Max($maxY, $y)
                }
            }
        }
        $relative = $file.FullName.Substring($sourceRoot.Length + 1).Replace('\', '/')
        if ($maxX -lt 0) { $empty += $relative; continue }
        # Retain one transparent pixel around the visible artwork, without rescaling.
        $left = [Math]::Max(0, $minX - 1); $top = [Math]::Max(0, $minY - 1)
        $right = [Math]::Min($image.Width - 1, $maxX + 1)
        $bottom = [Math]::Min($image.Height - 1, $maxY + 1)
        $entries += [pscustomobject]@{
            source = 'Sprites/Content/' + $relative
            sourceSha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
            spriteName = 'PBIcon_' + ($relative -replace '\.png$', '' -replace '/', '_')
            category = ($relative -split '/')[0]
            cropX = $left; cropY = $top
            width = $right - $left + 1; height = $bottom - $top + 1
            atlasX = 0; atlasTop = 0; atlasY = 0
        }
    } finally { $image.Dispose() }
}
$atlasWidth = 1024; $padding = 2; $cursorX = 2; $cursorY = 2; $rowHeight = 0
foreach ($entry in $entries | Sort-Object @{Expression='height';Descending=$true}, @{Expression='width';Descending=$true}, spriteName) {
    if ($cursorX + $entry.width + $padding -gt $atlasWidth) {
        $cursorX = $padding; $cursorY += $rowHeight + $padding; $rowHeight = 0
    }
    $entry.atlasX = $cursorX; $entry.atlasTop = $cursorY
    $cursorX += $entry.width + $padding; $rowHeight = [Math]::Max($rowHeight, $entry.height)
}
$atlasHeight = 1
while ($atlasHeight -lt $cursorY + $rowHeight + $padding) { $atlasHeight *= 2 }
$atlas = [System.Drawing.Bitmap]::new($atlasWidth, $atlasHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($atlas)
try {
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    foreach ($entry in $entries) {
        $image = [System.Drawing.Bitmap]::new((Join-Path $packRoot $entry.source))
        try {
            $destination = [System.Drawing.Rectangle]::new($entry.atlasX, $entry.atlasTop, $entry.width, $entry.height)
            $graphics.DrawImage($image, $destination, $entry.cropX, $entry.cropY, $entry.width, $entry.height, [System.Drawing.GraphicsUnit]::Pixel)
            $entry.atlasY = $atlasHeight - $entry.atlasTop - $entry.height
        } finally { $image.Dispose() }
    }
    $atlas.Save((Join-Path $outputRoot 'PaperBook_Icons_Atlas.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally { $graphics.Dispose(); $atlas.Dispose() }
@{
    atlas = 'SpriteSheets/Icons/PaperBook_Icons_Atlas.png'
    width = $atlasWidth; height = $atlasHeight; padding = $padding
    transparentMargin = 1; emptySources = $empty; sprites = $entries
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputRoot 'IconSlices.json') -Encoding utf8
[pscustomobject]@{ Icons = $entries.Count; EmptySources = $empty.Count; Atlas = "$atlasWidth x $atlasHeight" }

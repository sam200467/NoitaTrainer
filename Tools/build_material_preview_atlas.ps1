param(
    [Parameter(Mandatory = $true)] [string] $CatalogPath,
    [Parameter(Mandatory = $true)] [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common

function Get-NoitaWikiImages {
    param([string] $Prefix)

    $result = @()
    $continuation = $null
    do {
        $parameters = @{
            action   = 'query'
            list     = 'allimages'
            aiprefix = $Prefix
            ailimit  = '500'
            aiprop   = 'url|size'
            format   = 'json'
        }
        if ($null -ne $continuation) {
            $parameters.aicontinue = $continuation
        }
        $query = ($parameters.GetEnumerator() | ForEach-Object {
            [Uri]::EscapeDataString($_.Key) + '=' + [Uri]::EscapeDataString([string] $_.Value)
        }) -join '&'
        $response = Invoke-RestMethod -Uri ('https://noita.wiki.gg/api.php?' + $query) `
            -Headers @{ 'User-Agent' = 'NoitaTrainerAssetBuilder/1.0' }
        $result += $response.query.allimages
        $continuation = $response.continue.aicontinue
    } while ($null -ne $continuation)
    return $result
}

$catalog = Get-Content -LiteralPath $CatalogPath -Encoding UTF8 -Raw | ConvertFrom-Json
$wikiImages = @(Get-NoitaWikiImages -Prefix 'Material_')
$imageByMaterial = @{}
foreach ($wikiImage in $wikiImages) {
    $stem = [IO.Path]::GetFileNameWithoutExtension($wikiImage.name)
    if ($stem.StartsWith('Material_', [StringComparison]::OrdinalIgnoreCase)) {
        $materialId = $stem.Substring('Material_'.Length)
        $imageByMaterial[$materialId] = $wikiImage
    }
}

$cellSize = 64
$columns = 16
$rows = [Math]::Ceiling($catalog.materials.Count / $columns)
$atlas = [Drawing.Bitmap]::new($columns * $cellSize, $rows * $cellSize,
    [Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [Drawing.Graphics]::FromImage($atlas)
$http = [Net.Http.HttpClient]::new()
$http.DefaultRequestHeaders.UserAgent.ParseAdd('NoitaTrainerAssetBuilder/1.0')
try {
    $graphics.Clear([Drawing.Color]::Transparent)
    $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighSpeed
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::None

    for ($index = 0; $index -lt $catalog.materials.Count; $index++) {
        $material = $catalog.materials[$index]
        if (-not $imageByMaterial.ContainsKey($material.id)) {
            throw "Noita Wiki 缺少材质预览图：$($material.id)"
        }
        $wikiImage = $imageByMaterial[$material.id]
        $bytes = $http.GetByteArrayAsync([string] $wikiImage.url).GetAwaiter().GetResult()
        $stream = [IO.MemoryStream]::new($bytes, $false)
        try {
            $source = [Drawing.Image]::FromStream($stream)
            try {
                $x = ($index % $columns) * $cellSize
                $y = [Math]::Floor($index / $columns) * $cellSize
                $graphics.DrawImage($source,
                    [Drawing.Rectangle]::new($x, $y, $cellSize, $cellSize),
                    0, 0, $source.Width, $source.Height,
                    [Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $source.Dispose()
            }
        }
        finally {
            $stream.Dispose()
        }
    }

    $outputDirectory = Split-Path -Parent $OutputPath
    if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
        [IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
    }
    $atlas.Save($OutputPath, [Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $http.Dispose()
    $graphics.Dispose()
    $atlas.Dispose()
}

Write-Output "已生成 $($catalog.materials.Count) 张 Noita Wiki 材质预览图：$OutputPath"

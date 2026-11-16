[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet("search", "inspect", "download")]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$QueryOrId,

    [int]$Limit = 12,
    [int]$MaxTriangles = 3000,
    [switch]$IncludeAnimated,
    [switch]$Json
)

$ErrorActionPreference = "Stop"

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$ManifestPath = Join-Path $PSScriptRoot "asset-manifest.json"
$StagingRoot = Join-Path $PSScriptRoot "Staging\PolyPizza"
$ModelRoot = Join-Path $ProjectRoot "Assets\_GunVerse\Models\ThirdParty\PolyPizza"
$KeyPath = Join-Path $env:USERPROFILE ".config\gunverse\poly-pizza.key"
$ApiBase = "https://api.poly.pizza/v1.1"

function Get-PolyPizzaKey {
    if (-not [string]::IsNullOrWhiteSpace($env:POLY_PIZZA_API_KEY)) {
        return $env:POLY_PIZZA_API_KEY.Trim()
    }

    if (-not (Test-Path -LiteralPath $KeyPath)) {
        throw "Poly Pizza API key not found. Set POLY_PIZZA_API_KEY or create $KeyPath."
    }

    return (Get-Content -LiteralPath $KeyPath -Raw).Trim()
}

function Invoke-PolyPizzaApi {
    param([string]$Uri)

    $headers = @{ "x-auth-token" = (Get-PolyPizzaKey) }
    try {
        return Invoke-RestMethod -Uri $Uri -Headers $headers -Method Get
    }
    catch {
        throw "Poly Pizza API request failed for '$Uri'. Try a broader search keyword or use a model ID. $($_.Exception.Message)"
    }
}

function Get-PropertyValue {
    param(
        [object]$InputObject,
        [string[]]$Names
    )

    foreach ($name in $Names) {
        $property = $InputObject.PSObject.Properties[$name]
        if ($null -ne $property) {
            return $property.Value
        }
    }

    return $null
}

function ConvertTo-ModelRecord {
    param([object]$Model)

    return [pscustomobject]@{
        Id = Get-PropertyValue $Model @("ID", "Id", "id")
        Title = Get-PropertyValue $Model @("Title", "title")
        Creator = Get-PropertyValue (Get-PropertyValue $Model @("Creator", "creator")) @("Username", "username")
        Licence = Get-PropertyValue $Model @("Licence", "License", "licence")
        TriangleCount = Get-PropertyValue $Model @("Tri Count", "TriCount", "triCount")
        Animated = Get-PropertyValue $Model @("Animated", "animated")
        Category = Get-PropertyValue $Model @("Category", "category")
        Tags = Get-PropertyValue $Model @("Tags", "tags")
        Thumbnail = Get-PropertyValue $Model @("Thumbnail", "thumbnail")
        Download = Get-PropertyValue $Model @("Download", "download")
        Attribution = Get-PropertyValue $Model @("Attribution", "attribution")
    }
}

function Assert-CC0 {
    param([object]$Model)

    if ([string]::IsNullOrWhiteSpace($Model.Licence) -or $Model.Licence -notmatch "^CC0") {
        throw "Refusing to download non-CC0 model '$($Model.Title)' ($($Model.Id)): $($Model.Licence)"
    }
}

function Get-SafeName {
    param([string]$Value)

    $slug = ($Value.ToLowerInvariant() -replace "[^a-z0-9]+", "-").Trim("-")
    if ([string]::IsNullOrWhiteSpace($slug)) {
        return "model"
    }

    return $slug
}

function Write-ManifestRecord {
    param([object]$Record)

    $records = @()
    if (Test-Path -LiteralPath $ManifestPath) {
        $existing = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
        if ($null -ne $existing) {
            $records = @($existing)
        }
    }

    $records = @($records | Where-Object { $_.id -ne $Record.id })
    $records += $Record

    $json = ConvertTo-Json -InputObject @($records) -Depth 8
    [System.IO.File]::WriteAllText($ManifestPath, $json, (New-Object System.Text.UTF8Encoding($false)))
}

function Test-GlbFile {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -lt 12) {
        throw "Downloaded file is too small to be a GLB: $Path"
    }

    $magic = [System.Text.Encoding]::ASCII.GetString($bytes, 0, 4)
    if ($magic -ne "glTF") {
        throw "Downloaded file is not a valid GLB: $Path"
    }
}

if ([string]::IsNullOrWhiteSpace($QueryOrId)) {
    throw "Provide a search query or Poly Pizza model ID."
}

switch ($Command) {
    "search" {
        $encodedQuery = [uri]::EscapeDataString($QueryOrId)
        $response = Invoke-PolyPizzaApi "$ApiBase/search/$encodedQuery`?Limit=$Limit"
        $models = @($response.results | ForEach-Object { ConvertTo-ModelRecord $_ })

        $filtered = @($models | Where-Object {
            $_.Licence -match "^CC0" -and
            $_.TriangleCount -le $MaxTriangles -and
            ($IncludeAnimated -or -not $_.Animated)
        })

        if ($Json) {
            ConvertTo-Json -InputObject @($filtered) -Depth 6
            return
        }

        $filtered |
            Select-Object Id, Title, Creator, @{ Name = "Triangles"; Expression = { $_.TriangleCount } }, Licence, Animated, Category, Tags |
            Format-Table -AutoSize
        return
    }

    "inspect" {
        $model = ConvertTo-ModelRecord (Invoke-PolyPizzaApi "$ApiBase/model/$QueryOrId")
        if ($Json) {
            ConvertTo-Json -InputObject $model -Depth 6
            return
        }

        $model | Format-List
        return
    }

    "download" {
        $model = ConvertTo-ModelRecord (Invoke-PolyPizzaApi "$ApiBase/model/$QueryOrId")
        Assert-CC0 $model

        if ($model.TriangleCount -gt $MaxTriangles) {
            throw "Model '$($model.Title)' has $($model.TriangleCount) triangles, above the limit of $MaxTriangles."
        }

        $safeName = Get-SafeName $model.Title
        $folderName = "$safeName-$($model.Id)"
        $stagingFolder = Join-Path $StagingRoot $folderName
        $assetFolder = Join-Path $ModelRoot $folderName
        $fileName = "$safeName.glb"
        $stagingPath = Join-Path $stagingFolder $fileName
        $assetPath = Join-Path $assetFolder $fileName

        New-Item -ItemType Directory -Force -Path $stagingFolder | Out-Null
        New-Item -ItemType Directory -Force -Path $assetFolder | Out-Null

        if (-not (Test-Path -LiteralPath $stagingPath)) {
            $headers = @{ "x-auth-token" = (Get-PolyPizzaKey) }
            Invoke-WebRequest -Uri $model.Download -Headers $headers -OutFile $stagingPath -UseBasicParsing
        }

        Test-GlbFile $stagingPath
        Copy-Item -LiteralPath $stagingPath -Destination $assetPath -Force

        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        $stream = [System.IO.File]::OpenRead($stagingPath)
        try {
            $hashBytes = $sha256.ComputeHash($stream)
        }
        finally {
            $stream.Dispose()
            $sha256.Dispose()
        }
        $hash = ([BitConverter]::ToString($hashBytes) -replace "-", "").ToLowerInvariant()
        $unityAssetPath = "Assets/_GunVerse/Models/ThirdParty/PolyPizza/$folderName/$fileName"

        $record = [pscustomobject]@{
            id = $model.Id
            title = $model.Title
            creator = $model.Creator
            licence = $model.Licence
            attribution = $model.Attribution
            source = "PolyPizza"
            sourceUrl = "https://poly.pizza/m/$($model.Id)"
            downloadUrl = $model.Download
            triangleCount = $model.TriangleCount
            animated = $model.Animated
            sha256 = $hash
            assetPath = $unityAssetPath
            downloadedAtUtc = [DateTime]::UtcNow.ToString("o")
        }

        Write-ManifestRecord $record

        if ($Json) {
            ConvertTo-Json -InputObject $record -Depth 6
            return
        }

        $record | Format-List
        return
    }
}

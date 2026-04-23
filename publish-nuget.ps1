<#
.SYNOPSIS
    Build, pack, and push PDF2SVG.PopplerCairo.Bindings to NuGet.org.

.DESCRIPTION
    Single-project publisher for
        PDF2SVG.PopplerCairo.NetBindings/PDF2SVG.PopplerCairo.Bindings/PDF2SVG.PopplerCairo.Bindings.csproj
    (NuGet id: https://www.nuget.org/packages/PDF2SVG.PopplerCairo.Bindings/).

.PARAMETER ApiKey
    Your NuGet.org API key. Can also be set via the NUGET_API_KEY environment variable.

.PARAMETER Version
    Override the package version (e.g. "1.0.3").
    If omitted, uses the <Version> declared in the .csproj.

.PARAMETER Source
    NuGet feed URL. Defaults to https://api.nuget.org/v3/index.json

.PARAMETER SkipBuild
    Skip the dotnet build step (use when you have already built in Release).

.EXAMPLE
    .\publish-nuget.ps1 -ApiKey "oy2abc..."

.EXAMPLE
    .\publish-nuget.ps1 -Version 1.0.3

.EXAMPLE
    $env:NUGET_API_KEY = "oy2abc..."; .\publish-nuget.ps1
#>
[CmdletBinding()]
param(
    [string] $ApiKey   = $env:NUGET_API_KEY,
    [string] $Version  = "",
    [string] $Source   = "https://api.nuget.org/v3/index.json",
    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$csproj   = Join-Path $repoRoot "PDF2SVG.PopplerCairo.NetBindings/PDF2SVG.PopplerCairo.Bindings/PDF2SVG.PopplerCairo.Bindings.csproj"

if (-not (Test-Path $csproj)) {
    throw "Project file not found: $csproj"
}

# ── api key guard (once, up front) ───────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Error @"
No API key provided. Pass it with -ApiKey or set the NUGET_API_KEY environment variable:

    `$env:NUGET_API_KEY = "oy2..."
    .\publish-nuget.ps1
"@
}

# ── resolve version / package id ─────────────────────────────────────────────
[xml]$proj = Get-Content $csproj

$effectiveVersion = $Version
if ([string]::IsNullOrWhiteSpace($effectiveVersion)) {
    $effectiveVersion = $proj.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($effectiveVersion)) {
        throw "Could not read <Version> from '$csproj'. Pass -Version explicitly."
    }
}

$packageId = $proj.Project.PropertyGroup.PackageId | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($packageId)) {
    $packageId = $proj.Project.PropertyGroup.AssemblyName | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($packageId)) {
        $packageId = [System.IO.Path]::GetFileNameWithoutExtension($csproj)
    }
}

$projectDir = Split-Path -Parent $csproj
$nupkgDir   = Join-Path $projectDir "nupkg"
$nupkgFile  = Join-Path $nupkgDir "$packageId.$effectiveVersion.nupkg"

Write-Host ""
Write-Host "=== NuGet Publisher ===" -ForegroundColor Cyan
Write-Host "  Csproj   : $csproj"
Write-Host "  Package  : $packageId"
Write-Host "  Version  : $effectiveVersion"
Write-Host "  Feed     : $Source"
Write-Host ""

# ── 1. build ─────────────────────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "[1/3] Building in Release..." -ForegroundColor Yellow
    $buildArgs = @("build", $csproj, "-c", "Release", "--nologo", "/p:Version=$effectiveVersion")
    & dotnet @buildArgs | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Build failed for '$packageId'." }
} else {
    Write-Host "[1/3] Skipping build (-SkipBuild)." -ForegroundColor DarkGray
}

# ── 2. pack ──────────────────────────────────────────────────────────────────
Write-Host "[2/3] Packing..." -ForegroundColor Yellow

if (Test-Path $nupkgDir) { Remove-Item $nupkgDir -Recurse -Force }
New-Item -ItemType Directory -Path $nupkgDir | Out-Null

$packArgs = @(
    "pack", $csproj,
    "-c", "Release",
    "--no-build",
    "-o", $nupkgDir,
    "--nologo",
    "/p:Version=$effectiveVersion"
)
& dotnet @packArgs | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Pack failed for '$packageId'." }

if (-not (Test-Path $nupkgFile)) {
    $nupkgFile = Get-ChildItem $nupkgDir -Filter "*.nupkg" |
                 Where-Object { -not $_.Name.EndsWith(".symbols.nupkg") } |
                 Select-Object -First 1 -ExpandProperty FullName
    if (-not $nupkgFile) { throw "No .nupkg file found in '$nupkgDir' after pack." }
}

$sizeMB = [math]::Round((Get-Item $nupkgFile).Length / 1MB, 1)
Write-Host "  Produced : $nupkgFile ($sizeMB MB)" -ForegroundColor DarkGray

# ── 3. push ──────────────────────────────────────────────────────────────────
Write-Host "[3/3] Pushing to NuGet..." -ForegroundColor Yellow
& dotnet nuget push $nupkgFile `
    --api-key $ApiKey `
    --source $Source `
    --skip-duplicate | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Push failed for '$packageId'." }

Write-Host ""
Write-Host "Done! Package will appear on nuget.org within ~15 minutes." -ForegroundColor Green
Write-Host "  https://www.nuget.org/packages/$packageId/$effectiveVersion"
Write-Host ""

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Project = "Tascade/Tascade.csproj",
    [string[]]$Rids = @("win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"),
    [switch]$FrameworkDependent,
    [switch]$EnableSigning,
    [string]$CodeSignPfxPath = "",
    [string]$CodeSignPfxPassword = "",
    [string]$TimeStampUrl = "http://timestamp.digicert.com"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $Project)) {
    throw "Project file not found: $Project"
}

if ($EnableSigning) {
    if (-not (Test-Path -LiteralPath $CodeSignPfxPath)) {
        throw "Signing enabled but PFX file not found: $CodeSignPfxPath"
    }

    if ([string]::IsNullOrWhiteSpace($CodeSignPfxPassword)) {
        throw "Signing enabled but CodeSignPfxPassword is empty."
    }
}

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }
$root = Get-Location
$outRoot = Join-Path $root "artifacts/$Version"
$publishRoot = Join-Path $outRoot "publish"

if (Test-Path -LiteralPath $outRoot) {
    Remove-Item -LiteralPath $outRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

Write-Host "Building release assets for version: $Version"
Write-Host "Project: $Project"
Write-Host "Self-contained: $selfContained"
Write-Host "Signing enabled: $EnableSigning"

foreach ($rid in $Rids) {
    $publishDir = Join-Path $publishRoot $rid

    Write-Host "Publishing $rid..."
    $publishArgs = @(
        "publish", $Project,
        "-c", "Release",
        "-r", $rid,
        "--self-contained", $selfContained,
        "/p:PublishSingleFile=true",
        "/p:DebugType=None",
        "/p:DebugSymbols=false",
        "-o", $publishDir
    )

    if ($EnableSigning -and $rid.StartsWith("win-")) {
        $publishArgs += @(
            "/p:EnableSigning=true",
            "/p:CodeSignPfxPath=$CodeSignPfxPath",
            "/p:CodeSignPfxPassword=$CodeSignPfxPassword",
            "/p:TimeStampUrl=$TimeStampUrl"
        )
    }

    & dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for RID: $rid"
    }

    $archiveName = if ($rid.StartsWith("win-")) {
        "Tascade-$Version-$rid.zip"
    }
    else {
        "Tascade-$Version-$rid.tar.gz"
    }

    $archivePath = Join-Path $outRoot $archiveName

    if ($rid.StartsWith("win-")) {
        Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $archivePath -Force
    }
    else {
        tar -czf $archivePath -C $publishDir .
        if ($LASTEXITCODE -ne 0) {
            throw "tar packaging failed for RID: $rid"
        }
    }
}

Write-Host ""
Write-Host "Release assets created:"
Get-ChildItem -Path $outRoot -File | ForEach-Object { Write-Host " - $($_.FullName)" }

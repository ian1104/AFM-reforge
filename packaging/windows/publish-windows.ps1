[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Version = "0.0.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$appProject = Join-Path $repoRoot "src/AFMReforge.App/AFMReforge.App.csproj"
$profile = Join-Path $repoRoot "src/AFMReforge.App/Properties/PublishProfiles/win-x64.pubxml"
$staging = Join-Path $repoRoot "artifacts/windows/AFM-Reforge-Windows"
$zip = Join-Path $repoRoot "artifacts/windows/AFM-Reforge-win-x64-v$Version.zip"

if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
New-Item -ItemType Directory -Force -Path $staging | Out-Null

Write-Host "Publishing AFM Reforge ($Configuration / win-x64 / self-contained)..."
dotnet publish $appProject -c $Configuration -r win-x64 --self-contained true /p:PublishProfile=$profile -o $staging

Copy-Item (Join-Path $PSScriptRoot "README-PC-CAFE.md") $staging
Copy-Item (Join-Path $PSScriptRoot "START-AFM-REFORGE.bat") $staging

$manifest = @"
AFM Reforge Windows Package
Version: $Version
Configuration: $Configuration
RID: win-x64
Self-contained: true
Single-file: false
Trimmed: false
Runtime integration: UNVERIFIED
"@
Set-Content -Path (Join-Path $staging "PACKAGE-MANIFEST.txt") -Value $manifest -Encoding UTF8

Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -Force
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
Set-Content -Path (Join-Path $staging "SHA256.txt") -Value "$hash  $(Split-Path $zip -Leaf)" -Encoding ASCII

Write-Host "Artifact: $zip"
Write-Host "SHA256:   $hash"

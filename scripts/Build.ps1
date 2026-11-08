param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    dotnet run --project tests/Chroma.Tests -c $Configuration
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    dotnet publish src/Chroma.App -c $Configuration -r win-arm64 --self-contained true -o artifacts/win-arm64
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    $packagePath = Join-Path $projectRoot 'artifacts/MK-Saturation-1.0.0-win-arm64'
    New-Item -ItemType Directory -Force $packagePath | Out-Null
    Copy-Item -LiteralPath 'artifacts/win-arm64/MK Saturation.exe' -Destination $packagePath -Force
    Copy-Item -LiteralPath 'README.md','scripts/Install.ps1','scripts/Uninstall.ps1' -Destination $packagePath -Force
    $exeHash = Get-FileHash -LiteralPath "$packagePath/MK Saturation.exe" -Algorithm SHA256
    "$($exeHash.Hash)  MK Saturation.exe" | Set-Content -LiteralPath "$packagePath/SHA256SUMS.txt"
    Compress-Archive -Path "$packagePath/*" -DestinationPath 'artifacts/MK-Saturation-1.0.0-win-arm64.zip' -Force
    Get-FileHash -LiteralPath 'artifacts/MK-Saturation-1.0.0-win-arm64.zip' -Algorithm SHA256 | Format-List
} finally { Pop-Location }

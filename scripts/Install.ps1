param([string]$SourceExe = (Join-Path $PSScriptRoot 'MK Saturation.exe'))
$ErrorActionPreference = 'Stop'
if (Get-Process 'MK Saturation' -ErrorAction SilentlyContinue) { throw 'Close MK Saturation before installing.' }
$installFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs/MK Saturation'
New-Item -ItemType Directory -Force $installFolder | Out-Null
$destinationExe = Join-Path $installFolder 'MK Saturation.exe'
if ([IO.Path]::GetFullPath($SourceExe) -ne [IO.Path]::GetFullPath($destinationExe)) { Copy-Item -LiteralPath $SourceExe -Destination $destinationExe -Force }
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'MK Saturation.lnk'
$shellObject = New-Object -ComObject WScript.Shell
$shortcut = $shellObject.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $destinationExe
$shortcut.WorkingDirectory = $installFolder
$shortcut.Save()
Write-Output "Installed to $destinationExe. Launch from the Start menu."

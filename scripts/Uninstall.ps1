param([switch]$RemoveSettings)
$ErrorActionPreference = 'Stop'
if (Get-Process 'MK Saturation' -ErrorAction SilentlyContinue) { throw 'Close MK Saturation first so it can restore colors.' }
Remove-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'MK Saturation' -ErrorAction SilentlyContinue
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'MK Saturation.lnk'
if (Test-Path -LiteralPath $shortcutPath) { Remove-Item -LiteralPath $shortcutPath }
if ($RemoveSettings) {
    $settingsPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'MK Saturation/settings.json'
    if (Test-Path -LiteralPath $settingsPath) { Remove-Item -LiteralPath $settingsPath }
}
Write-Output 'Startup registration and shortcut removed. Delete the portable executable when ready.'

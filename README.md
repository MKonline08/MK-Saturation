# MK Saturation

A native Windows 11 ARM64 desktop application for adjusting saturation on Snapdragon X / Qualcomm Adreno PCs. WPF, C# and .NET 10; no vendor GPU SDK or administrator privileges required.

## Download and run

Download **MK Saturation.exe** from this repository's Releases. Put it in a permanent folder (for example, `%LOCALAPPDATA%\Programs\MK Saturation`) and double-click it. The self-contained ARM64 executable includes the .NET runtime. It is unsigned; no signing certificate is bundled. This build runs on ARM64 Windows 11, not x64 Windows.

1. Move the slider from **0–200%**. **100% is neutral**, 0% is grayscale. Values above 100% can clip colors.
2. The default is **local preview**. Enable **Apply to the Windows desktop** to request a global effect. The app reports API acceptance and reads the matrix back; inspect your desktop to confirm visible coverage.
3. **Compare original** temporarily selects the neutral matrix; click again to return. **Reset to 100%** also updates the saved preference.
4. **Hide to tray** keeps the effect running. Double-click the tray icon to reopen. Closing the window or choosing **Restore and exit** restores the captured neutral baseline and quits.
5. Enable **Launch when I sign in** to register this executable in your own Windows account. **Start in the tray** applies only to startup launches. Preferences save automatically.

The application pauses desktop control after lock, logoff, remote disconnect or sleep. Enable it again after returning. Windows may disable startup entries independently in Task Manager; a registry entry alone does not override that Windows choice.

## What the global effect can and cannot do

The backend calls `MagInitialize`, `MagGetFullscreenColorEffect`, `MagSetFullscreenColorEffect` and `MagUninitialize` from Windows' `Magnification.dll`. It uses a row-vector 5×5 matrix with Rec.709 weights (0.2126, 0.7152, 0.0722); alpha is unchanged. This is a saturation matrix, not NVIDIA's content-adaptive Digital Vibrance algorithm or a display calibration profile.

Microsoft documents fullscreen color effects on Windows 8 and later and notes that the Magnification API does not support WOW64. This package targets **native ARM64** to avoid emulation. The documentation does not provide a Snapdragon model/driver compatibility guarantee. Native initialization and matrix read/write must succeed at runtime. The app does not use `MagSetInputTransform`, request UIAccess, or change magnification/zoom, display profiles, gamma ramps or GPU-driver settings.

**API success is not a measurement of the pixels reaching your panel.** HDR/Advanced Color, hardware overlays, protected video, remote sessions and exclusive fullscreen games require separate visual testing. Treat coverage in those paths as unverified. Borderless windowed mode may behave differently. This effect is desktop-wide; there is no per-monitor, per-app or game exclusion support. Other Magnifier/color applications share the same fullscreen matrix and can conflict.

If initialization, setting, reading or verification fails, global control pauses and the app shows the specific error. The software swatch preview remains usable and is explicitly labeled local. There is **no hidden overlay, driver injection, unsupported Qualcomm API, or promise of an alternative global backend**. The **Display settings** button opens Windows' display settings for supported device-specific alternatives. Retry after closing competing tools. Existing non-neutral fullscreen matrices are left untouched, rather than overwritten. While active, polling detects changes made by other apps and relinquishes ownership. A narrow race between reading and writing a shared Windows API remains possible; do not run competing color tools together.

Normal exit restores only an effect the app still owns. If a different app changed the matrix, its matrix is left intact. If restoration fails, the UI offers retry/cancel or exit with an explicit warning. Forced termination, power loss and native process crashes cannot guarantee restoration; **sign out and back in** if colors remain altered. Saved preferences are not a crash-recovery copy of another application's matrix.

## Settings, startup and removal

- Preferences: `%LOCALAPPDATA%\MK Saturation\settings.json`, written by atomic file replacement. Invalid JSON falls back to defaults with a warning; percentages are clamped.
- Startup: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value `MK Saturation`, containing the quoted executable path plus `--startup`. No scheduled task or service is installed. Moving the executable requires toggling startup off/on at its new location.
- One instance per interactive Windows session. Open a running instance from the tray.
- Remove: turn off startup, close the app, and delete its executable/folder. Delete the settings folder if you want to remove preferences. The optional uninstall script performs the registry/settings cleanup after the app has exited.
- No network calls, telemetry or updater in the app. Diagnostics export only when requested. Review exception text before sharing diagnostics.

## Build and package

Install the official **.NET 10 SDK** on Windows (ARM64 SDK on ARM64; Windows x64 can cross-publish). A Windows desktop targeting pack is restored by the SDK. There are no third-party NuGet dependencies. Run from the repository root:

```powershell
dotnet run --project tests/Chroma.Tests -c Release
dotnet build src/Chroma.App -c Release
dotnet publish src/Chroma.App -c Release -r win-arm64 --self-contained true -o artifacts/win-arm64
```

Or run `./scripts/Build.ps1`. The app project and `Properties/PublishProfiles/WindowsArm64.pubxml` configure ARM64, self-contained, untrimmed single-file publish with native libraries extracted at runtime. The output **MK Saturation.exe** can be distributed alone. PDBs are debugging files and are excluded from release packages. The executable is relatively large because it includes WPF and the runtime. `Build.ps1` also produces a portable ZIP and SHA-256 checksums. The ZIP includes the README and optional install/uninstall scripts; an installer is not required.

`./scripts/Install.ps1 -SourceExe 'path\MK Saturation.exe'` copies the portable executable into a per-user program folder and creates a Start menu shortcut, without enabling startup or changing display settings. Run only after closing existing instances. No machine-wide changes or admin access are required.

## Diagnostics and tests

**Save diagnostics** exports current architecture, OS, selected value, ownership, matrix and last error without changing the effect. It uses the existing initialized backend. Readback cannot certify visual coverage.

For automated native checks, first exit the GUI:

```powershell
Start-Process '.\artifacts\win-arm64\MK Saturation.exe' -ArgumentList '--diagnose', 'C:\Temp\mk-report.txt' -Wait
Start-Process '.\artifacts\win-arm64\MK Saturation.exe' -ArgumentList '--native-smoke', 'C:\Temp\mk-smoke.txt' -Wait
```

Use a quoted report-path argument when it contains spaces. The output directory must already exist. `--diagnose` initializes and reads only; `--native-smoke` writes the unchanged captured matrix, verifies it and restores it in `finally`. Exit code 0 means pass, 1 backend failure, 2 report-write failure. These commands do not run alongside another instance.

The dependency-free test runner checks matrix orientation, identity, grayscale, gray preservation, clipping, bounds, nonfinite comparison, ownership, competing effects, write/readback failures, restoration retry, settings clamping, round trip and corrupt fallback. Run it with `dotnet run`, not `dotnet test`. See [validation checklist](docs/VALIDATION.md) for native and visual acceptance checks and the actual validation record. CI compiles and tests on Windows and publishes an ARM64 artifact; cross-publishing in CI does not exercise an Adreno driver.

## Primary references

- [Magnification API overview and lifecycle](https://learn.microsoft.com/en-us/windows/win32/winauto/magapi/magapi-intro)
- [MagSetFullscreenColorEffect](https://learn.microsoft.com/en-us/windows/win32/api/magnification/nf-magnification-magsetfullscreencoloreffect)
- [MagGetFullscreenColorEffect](https://learn.microsoft.com/en-us/windows/win32/api/magnification/nf-magnification-maggetfullscreencoloreffect)
- [Magnification API runtime requirements / WOW64 limitation](https://learn.microsoft.com/en-us/windows/win32/winauto/magapi/entry-magapi-sdk)

No license to redistribute this project's source is granted beyond GitHub's standard platform terms unless the repository owner adds a license. Microsoft runtime components retain their own licenses bundled in the executable.

# Validation record and acceptance checklist

## Actual local validation — November 7, 2026 (America/Chicago)

- Windows 11 build 26200, native ARM64 process and OS.
- GPU identified locally as Qualcomm Adreno X1-45, driver 31.0.178.0.
- .NET SDK 10.0.403, Release build: zero warnings and errors.
- Dependency-free automated test runner: 13 tests passed.
- Self-contained win-arm64 single executable published successfully.
- Native smoke: initialization and initial matrix read succeeded; initial matrix was identity. Writing that unchanged matrix and reading it back succeeded. Original matrix restored in finally.
- GUI opened on the ARM64 machine; the slider, reset and global/local toggle were exercised. At 0%, the UI reported that Windows accepted the desktop matrix. Reset and disable returned to 100% local mode before exit.
- Window screenshots retain color at 0% global saturation. Window capture may occur before fullscreen composition, so this **does not prove either visible panel success or failure**. Physical-panel confirmation is still required. HDR, protected video, exclusive games, multi-monitor coverage, startup after an actual sign-in, sleep/lock and remote-session paths have not been certified.

## Manual acceptance before claiming hardware coverage

1. Begin with competing color tools closed, SDR, ordinary local desktop. Launch without administrator privileges. Confirm native ARM64 in Task Manager or diagnostics.
2. In local mode, test 0, 100 and 200. Only the swatches should change. Use slider arrow keys, Page Up/Down, Home/End. Resize and scroll; all controls must remain reachable at 100%, 150% and 200% display scaling.
3. Enable desktop mode at 0%. Check the physical panel: Start menu, Settings, browser and app swatches should visibly turn gray where supported. Ask another observer or use a camera if needed; a window screenshot alone is insufficient.
4. Test 100 and 200. Compare original should temporarily return neutral without changing the saved percentage. Reset must select and save 100. Disable global mode and exit; check the panel returns to baseline.
5. Try HDR on/off, SDR and HDR video, DRM video, borderless and exclusive games, internal/external displays and remote sessions separately. Record each result and driver/Windows version. Never infer these from the ordinary desktop result.
6. Hide to tray, reopen by double-click, reset from tray and restore/exit. Relaunch; selected percentage and global-mode choice should persist. Launch a second copy; it should report the existing instance.
7. Enable startup from a permanent executable path. Check the current-user Run value is quoted and points to that path. Sign out/in, verify saved behavior and optional startup tray launch. Disable and verify the value is removed. Check Task Manager startup overrides separately.
8. Lock/unlock, sleep/resume and disconnect remote access: desktop control should pause. No automatic fight with another color app after resume.
9. Use another test tool to set a different matrix while active; polling should pause global mode. Exiting must leave the new matrix untouched. Existing non-neutral matrix at startup should prevent acquisition.
10. Corrupt settings JSON with the app closed; relaunch should use defaults and show a warning. Try a read-only settings folder and diagnostics destination; failures must be shown without a crash.
11. Simulate API failures with the fake backend tests. On unsupported native environments, verify local fallback and retry messaging. Force termination only on a disposable test session; if colors remain changed, sign out to recover.

The automated controller tests cover shared-matrix ownership, rollback, and restoration retry. UI and physical display checks are complementary and are not replaced by CI.

# Release validation

## Windows 0.5.5 preview

The Windows update passes the existing integration, sharing, window, encrypted transfer, and desktop suites described below, plus:

- 42 continuity regression checks: bounded offline retry, explicit retry status, offline removal persistence and stale-snapshot filtering, pause persistence, receiver-driven microphone requests, missing cable, source permission denial, independent microphone selection for multiple receivers, simultaneous send/receive, rapid replacement, stale errors/stops, cancellation before playback starts, unplug/disconnect behavior, exact vertical/horizontal wheel deltas, silent handoffs, and gesture packet validation. Routing tests use fake audio devices; the separate sharing suite still exercises real WinMM capture and silent playback.
- 39 native Precision Touchpad checks on this Windows 11 25H2 build (26200): synthetic two-, three-, and four-finger contacts are captured through Windows APIs, serialized, and accepted by the production injector, including all finger releases. These are synthetic-input checks, not physical trackpad acceptance or a two-PC latency test.
- 6 native source handoff checks verify the production InputController capture surface forwards two-, three-, and four-finger frames and restores foreground focus on return, and exits remote control when capture loses foreground focus.
- Updated microphone dialog and desk/device layouts rendered and visually inspected at the tested window sizes.

Run `build.ps1 -WindowsOnly`, then `tests/run-tests.ps1`. The native gesture test temporarily focuses a small test surface and restores the previous foreground window and cursor. The bridge is compiled separately from Windows WinMetadata so older runtime Windows versions can retain mouse/keyboard support without loading unsupported WinRT types. Building the bridge requires an updated Windows 11 build with the newer API metadata.

Release outputs: `dist/Velixa-0.5.5-Windows-Setup.exe`, `dist/Velixa-0.5.5-Windows-Portable.zip`, and `dist/Velixa-0.5.5-Android.apk`; hashes are in `dist/SHA256SUMS-0.5.5.txt`. Windows, installer, and Android version agreement is enforced by the build.

Physical acceptance still required on both updated PCs: actual touchpad scroll/pinch/swipe/tap behavior using different receiving-PC gesture settings, source focus restoration, fast edge handoffs, USB/Bluetooth hotplug, microphone audio through an installed virtual cable into the intended calling app, and mixed-DPI layouts. No virtual audio cable is installed on the test PC, so app-level microphone routing was not physically verified.

Implementation references: [Microsoft Precision Touchpad guide](https://learn.microsoft.com/en-us/windows/win32/input-precisiontouchpad/precision-touchpad-guide), [synthetic pointer device API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createsyntheticpointerdevice2).

## Additional Windows 0.5.5 validation

- 229 integration checks for authentication, input routing, source switching, saved desks, sleep, sizing and shared edge geometry.
- 9 core cryptography, framing and storage checks.
- 24 sharing checks for files, clipboard, cancellation, corruption, microphone capture/playback, negotiation and shutdown.
- 218 desktop checks cover navigation, action availability, persistent preferences, zoom and Fit, device operations, microphone routing, four window sizes, and repeated-scroll background pixel checks.
- 30 minimize/close/restore cycles and 8 actual injected title-bar clicks. The click harness detects physical-pointer interference rather than misclassifying it as a product defect.
- An 8 MB file passes through two authenticated TLS clients and a coordinator, with exact bytes/hash and a live heartbeat verified.
- Size, microphone and settings dialogs rendered and inspected; desk layout inspected at four tested sizes.

Run `build.ps1 -WindowsOnly`, then `tests/run-tests.ps1`. Test harnesses are compiled with TESTING and require a unique VELIXA_TEST_DATA directory. They use TCP 47128 and UDP 47129; missing or production storage paths are rejected. Preview and self-test modes use temporary settings instead of the installed desk.

## Android 0.5.5

22 emulator checks cover pairing, layout, reconnection, sleep/wake, microphone enumeration visible service lifecycle, correlated microphone request replacement, stale/current stop handling, and silent edge resets. Additional checks cover shared device numbering and repeated scrolling in both directions, sampling scroll position to detect reversals. The Windows coordinator verified receipt of 151 Android PCM audio packets. Twelve JVM regression checks cover built-in route deduplication, non-microphone exclusion, and distinct external microphones; run tests/android-routes.ps1 with -AndroidSdk. The release APK is checked with apksigner and retains the existing signing identity.

Instrumentation is destructive to the test app's state and must run only on a disposable emulator. Build and start tests/EmulatorHost.cs with TESTING and isolated storage, map the emulator's 10.0.2.2:37128 traffic to the isolated coordinator at 47128, install the release APK, grant test microphone permission, then run tests/android-smoke.ps1 with explicit -Serial and -AndroidSdk. Never redirect a physical device or use the installed production coordinator for QA.

Android theme instrumentation (`tests/android-theme.ps1`) renders setup, pairing, connected and scrolled layouts. The connected visual fixture is separate from authenticated pairing validation. Twelve microphone-route checks passed. APK v2/v3 signatures are verified during the build.

## Remaining physical-device acceptance

Automated checks do not verify every physical laptop, phone, driver or microphone. Mixed-DPI multi-monitor handoff, OEM background restrictions, external/Bluetooth microphone routing, and shared audio selected inside third-party calling apps require real-device acceptance. A separately installed virtual audio cable is required for Windows apps to see incoming audio as a microphone. Android does not receive Windows microphone audio for phone calls.

This is a preview release, not a guarantee of zero defects. The Windows installer is unsigned.

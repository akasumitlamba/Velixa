# Release validation

## Windows 0.5.1

- 229 integration checks for authentication, input routing, source switching, saved desks, sleep, sizing and shared edge geometry.
- 9 core cryptography, framing and storage checks.
- 24 sharing checks for files, clipboard, cancellation, corruption, microphone capture/playback, negotiation and shutdown.
- 29 desktop QA checks cover fail-closed test storage, desk switching and removal, microphone discovery without echo, remote microphone permission, simultaneous microphone packets to three receivers, correctly targeted drops, full device labels, and aligned controls at 1200×850 and 920×690 client sizes.
- 30 minimize/close/restore cycles and 8 actual injected title-bar clicks. The click harness detects physical-pointer interference rather than misclassifying it as a product defect.
- An 8 MB file passes through two authenticated TLS clients and a coordinator, with exact bytes/hash and a live heartbeat verified.
- Size, microphone and settings dialogs rendered and inspected; desk layout inspected at both tested sizes.

Run `build.ps1 -WindowsOnly`, then `tests/run-tests.ps1`. Test harnesses are compiled with TESTING and require a unique VELIXA_TEST_DATA directory. They use TCP 47128 and UDP 47129; missing or production storage paths are rejected. Preview and self-test modes use temporary settings instead of the installed desk.

## Android 0.5.1

16 emulator checks cover pairing, layout, reconnection, sleep/wake, microphone enumeration and visible service lifecycle. Additional checks cover shared device numbering and repeated scrolling in both directions, sampling scroll position to detect reversals. The Windows coordinator verified receipt of 96 Android PCM audio packets. Twelve JVM regression checks cover built-in route deduplication, non-microphone exclusion, and distinct external microphones; run tests/android-routes.ps1 with -AndroidSdk. The release APK is checked with apksigner and retains the existing signing identity.

Instrumentation is destructive to the test app's state and must run only on a disposable emulator. Build and start tests/EmulatorHost.cs with TESTING and isolated storage, map the emulator's 10.0.2.2:37128 traffic to the isolated coordinator at 47128, install the release APK, grant test microphone permission, then run tests/android-smoke.ps1 with explicit -Serial and -AndroidSdk. Never redirect a physical device or use the installed production coordinator for QA.

## Remaining physical-device acceptance

Automated checks do not verify every physical laptop, phone, driver or microphone. Mixed-DPI multi-monitor handoff, OEM background restrictions, external/Bluetooth microphone routing, and shared audio selected inside third-party calling apps require real-device acceptance. A separately installed virtual audio cable is required for Windows apps to see incoming audio as a microphone. Android does not receive Windows microphone audio for phone calls.

This is a preview release, not a guarantee of zero defects. The Windows installer is unsigned.

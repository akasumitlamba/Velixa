# Velixa 0.2 automated validation

Release artifacts are built from the current Windows and Android sources. Manual device acceptance is left to the user.

## Automated coverage

- 25 integration assertions: short-lived four-digit codes, incorrect-code rejection, SRP mutual authentication, per-device token issuance, saved-token reconnection, identity binding, single-use QR pairing, expiry, attempt limits, heartbeat expiry, invalid pointer values, horizontal and vertical edge geometry, non-overlapping boundaries, offline-device exclusion, source routing, Android receive-only enforcement, stale-source rejection, layout synchronization, and retained offline devices.
- 9 core checks: HMAC known vector, comparison behavior, Unicode wire format, oversized-frame rejection, TLS identity and encrypted settings round trips.
- 6 Android instrumentation checks on a disposable Android 15 emulator: decoding the Windows-generated QR, authenticating over the production protocol, receiving the shared desk, sending X/Y positions and receiving the result, exchanging the QR token for a saved credential, and pinned reconnection.
- Native Windows overlay rectangles checked against the actual monitor bounds for all four edges earlier in this work.
- APK signature schemes v2/v3 verified; Android updates reuse the existing signing identity.
- Windows EXE and installer compilation, installer contents, and release hashes verified.

The automated Windows render fixtures are isolated from saved desk state. They do not represent real connected devices. No further manual desktop testing was performed after the user requested automated checks only.

## Acceptance left to the user

Physical-device camera scanning, mixed-DPI multi-monitor movement, Windows-to-Windows source handoff on separate laptops, reconnect prompts after real power cycles, Android OEM battery behavior, and app-specific typing remain manual acceptance tasks. The coordinator PC must remain awake. The Windows installer is unsigned. This is a preview release.

Run `tests/run-tests.ps1` for Windows/integration assertions. Android instrumentation uses `tests/EmulatorHost.cs`, `tests/android-smoke.ps1`, and only the emulator identified explicitly as `emulator-5580`.

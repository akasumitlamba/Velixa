# Velixa 0.4.0 Windows automated validation

## Current coverage

- 229 integration checks: authentication, routing, source selection, saved desks, sleep, sizing, shared edge segments, randomized non-overlapping placements, and sharing capability authorization.
- 9 core cryptography, framing and storage checks.
- 30 close/minimize/restore cycles with active input hooks and receiver; input injection remains responsive while the UI thread is blocked.
- 8 actual injected title-bar minimize/close clicks, verifying the receiver and session remain alive.
- 24 sharing checks: binary and empty files, duplicate names, clipboard text/images, checksum rejection, cancellation, path validation, temporary-file cleanup, microphone permissions, native audio capture/output, negotiation and stream shutdown.
- An 8 MB file transferred through two authenticated TLS clients and the coordinator, with byte/hash verification and a working heartbeat. Final local test took 2065 ms; this is not a real-network speed guarantee.
- Three rendered dialogs checked, including screen-size slider/value separation; transfer panel inspected.
- Installer compilation, portable archive contents and SHA-256 checksums verified.

Android 0.3.0 is unchanged and previously passed 10 emulator checks. New clipboard, file and microphone features are Windows-only.

## Run the checks

Run `build.ps1 -WindowsOnly`, then `tests/run-tests.ps1`. Tests use isolated data and ports rather than the installed desk.

## Physical-device acceptance

Real multi-PC input handoff, mixed-DPI movement, OEM sleep behavior and microphone selection in third-party applications still require physical-device acceptance. Application microphone input requires a separately installed virtual audio cable. The coordinator must remain awake. The Windows installer is unsigned. This is a preview release.

# Velixa 1.1.0 local review build

This change preserves the existing file-drop work in the checkout. No GitHub release has been published and the installed application has not been replaced.

## Connection changes

- Saved Windows and Android pairings retry indefinitely with a bounded delay instead of stopping after three failures. A new pairing attempt still fails promptly when its code is invalid.
- Authenticated connections tolerate 30 seconds of missed heartbeats; read timeouts remain bounded. Retry cancels the previous pending socket.
- Discovery sends broadcasts on active IPv4 interfaces so a VPN or another adapter is less likely to hide the LAN coordinator.
- Turning off a Windows display no longer marks the PC asleep. Actual system suspend/resume still updates availability.
- Android has a foreground connected-device service and bounded, renewed wake-lock leases while explicitly connected. Disconnect releases it. Opening the activity can restart an offline connection.
- Disposed desk sessions ignore queued callbacks, preventing old session updates from overwriting a replacement session.

The desk-creator PC is still the coordinator. Other devices cannot communicate through a coordinator that is powered off. Automatic recovery resumes when it returns; this is not coordinator failover.

## Desk and window changes

- Removed the left navigation and its microphone, pause, and disconnect controls. The desk uses the freed space.
- Added current-PC Media, Clipboard, and File sharing switches; media selectors are hidden while media sharing is off.
- The selected-device inspector exposes screen size, disconnect/wake, and removal from this desk.
- Current-PC hardware controls select the mouse/keyboard source and the physical microphone/speaker used by consumers. Quit app stays at the bottom and asks for Quit/Cancel confirmation.
- Zoom and Fit are inside the canvas at bottom right. Removed the drag-layout help banner.
- Desks retain independent membership, position, scale, and manual disconnect state. New desks begin with this PC; Add device can include saved peers without pairing again. Removal affects the current desk.
- Offline screens collapse to thin placeholders while saved geometry is retained. Input can traverse the offline gap to the next available screen.
- Minimize remains in the taskbar. Close hides to tray. Quit stops the session.

## Media implementation

- Microphone source and speaker destination are independent per-PC choices. The source PC owns the physical microphone selection. Multiple consumers can request it.
- Speaker streams resolve to the final selected destination; cycles and unavailable destinations are rejected. Multiple streams play on the destination's selected physical output.
- Windows audio defaults are journaled before redirection and restored on stop. VB-CABLE endpoints are hidden while unused. A separate process restores them after an unexpected main-process exit. This adapter is disabled in ordinary automated test/preview builds. The explicit audio-policy runner uses isolated settings and restores the physical mute state after testing.
- File/clipboard switches now block outbound work as well as inbound work and cancel the respective active transfers.

Windows speaker capture now uses an independent process-loopback bridge that excludes the Velixa process tree. It captures other applications before endpoint mute, while VB-CABLE carries only the remote microphone. Both routes can operate at the same time without playing the outgoing speaker mix locally. Physical output mute states are journaled before changes and restored on disconnect/off/exit. The guard also restores them after an abrupt process exit. Destination playback follows its selected physical output, or the Windows default when no device is selected.

The native bridge uses Microsoft's process-loopback API (Windows build 20348 or newer; Windows 11 for consumer PCs). Protected or exclusive-mode playback may not be captured. Applications pinned to a specific microphone may not follow changes to the Windows default.

A hardware-backed synthetic-tone test on this PC verified capture with the physical endpoint muted and exclusion of audio played by the capturing process. A separate abrupt-exit test verified restoration of original physical mute states by the guard. Bluetooth routing, real multi-PC mixing, and latency still need physical device acceptance.

## Android pairing

Code entry accepts the PC's local IPv4 address and four-digit code. QR pairing remains available. Android uses the same SRP-6a group, SHA-256 evidence, server confirmation, saved token, and certificate pinning as Windows. No root, Shizuku, or developer setup is added.

## Verification

Run `./build.ps1 -AndroidSdk <sdk>` and `./tests/run-tests.ps1`. Additional focused runners are `./tests/revision-tests.ps1`, `./tests/android-routes.ps1 -AndroidSdk <sdk>`, and `./tests/audio-loopback.ps1`. The audio runner temporarily mutes output, plays synthetic tones, and tests crash recovery.

The delayed-host recovery test starts the host only after at least four failed attempts, verifies automatic authenticated reconnection, checks idle heartbeats, breaks the established socket, and verifies recovery again. Desk regressions cover independent membership, saved positions, removal, manual disconnect, horizontal/vertical gaps, and speaker cycle rejection. Java/.NET SRP interoperability covers three codes including a leading zero.

Build/test results and artifact hashes are recorded in `docs/desk-reliability-1.1.0-validation.txt`. UI renders are under `build/qa-*.png`. No Android device was attached during this work; APK compilation and protocol tests do not prove physical phone sleep/wake or camera scanning. Synthetic touchpad tests do not establish physical trackpad acceptance.

## Implementation references

- [Android connected-device foreground service requirements](https://developer.android.com/develop/background-work/services/fgs/service-types#connected-device)
- [Microsoft process-loopback sample and API requirements](https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/)
- [Microsoft audio endpoint visibility discussion](https://learn.microsoft.com/en-au/answers/questions/1617393/how-to-enable-disable-audio-devices)

NAudio 1.10.0 is distributed with its Microsoft Public License in the installer and portable package.

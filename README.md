# Velixa

### One keyboard and mouse. Every screen on your desk.

Velixa lets Windows PCs share a keyboard, mouse, clipboard, files, and microphones across a local desk. Move the pointer across the edge of one screen to control another Windows or Android device, with no account and no cloud relay.

[![Latest Windows release](https://img.shields.io/badge/Windows-0.5.4_preview-0078D4?logo=windows)](https://github.com/akasumitlamba/Velixa/releases/tag/v0.5.4-preview)
[![Latest Android release](https://img.shields.io/badge/Android-0.5.4_preview-3DDC84?logo=android&logoColor=white)](https://github.com/akasumitlamba/Velixa/releases/tag/v0.5.4-preview)
[![License: MIT](https://img.shields.io/badge/License-MIT-F4B942.svg)](LICENSE)

![Velixa desk with aligned controls, gold local screen and purple remote screens](docs/desk.png)

## Why Velixa?

- **Seamless control:** move naturally between devices by crossing a screen edge.
- **Local by design:** traffic stays on your LAN over authenticated, encrypted connections.
- **No account required:** no sign-in, subscription, advertising, or cloud relay.
- **Flexible desk layouts:** arrange screens to match your physical setup and save up to 12 layouts.
- **More than input sharing:** sync Windows clipboards, transfer files, and route microphone audio between supported devices.

## Platform support

| Capability | Windows | Android |
| --- | :---: | :---: |
| Receive keyboard and mouse input | Yes | Yes |
| Act as an input source | Yes | No |
| Text and image clipboard sharing | Yes | Not yet |
| File transfer | Yes | Not yet |
| Share microphone audio | Yes | Yes |
| Receive microphone audio | Yes | No |

> [!NOTE]
> Windows and Android are aligned at 0.5.4. This update fixes Windows scrolling backgrounds and gives Android a matching dark theme and cleaner layout. Existing pairings are retained.

## Download

| Download | Use |
| --- | --- |
| [Windows installer](https://github.com/akasumitlamba/Velixa/releases/download/v0.5.4-preview/Velixa-0.5.4-Windows-Setup.exe) | Install Velixa on a Windows PC. |
| [Windows portable ZIP](https://github.com/akasumitlamba/Velixa/releases/download/v0.5.4-preview/Velixa-0.5.4-Windows-Portable.zip) | Extract the complete folder and run `Velixa.exe`. Keep the included dependencies beside it. |
| [Android APK](https://github.com/akasumitlamba/Velixa/releases/download/v0.5.4-preview/Velixa-0.5.4-Android.apk) | Install or update Velixa on an Android device. |
| [SHA-256 checksums](https://github.com/akasumitlamba/Velixa/releases/download/v0.5.4-preview/SHA256SUMS-0.5.4.txt) | Verify the downloaded files. |

Windows requires .NET Framework 4.8. The installer is unsigned, so Windows may display an unknown-publisher warning. Android requires Android 8 or later; the APK retains the project's existing signing identity for upgrades.

See the [0.5.4 Preview release notes](https://github.com/akasumitlamba/Velixa/releases/tag/v0.5.4-preview). This is a preview, with hardware-specific acceptance still required.

## Quick start

1. Install the Windows app on each PC and the APK on each Android device.
2. On one Windows PC, select **Create my desk**, then **Add device**. This PC becomes the coordinator and must remain awake.
3. On another Windows PC, find the desk and enter the four-digit pairing code.
4. On Android, enable Velixa continuity in Accessibility, then scan the QR code shown under **Add device > Android** on the coordinator.
5. Drag the screen cards into the same arrangement as your physical devices.
6. Move the pointer across a touching screen edge to take control of the next device.

If a pairing code expires, select **New pairing code** in the Windows pairing dialog. Both devices must be on the same reachable local network.

## Using your desk

### Windows navigation

| Page | What you can do |
| --- | --- |
| **My Desk** | Arrange device cards, select the input source, pause input sharing, toggle clipboard sharing, and zoom or fit the view. |
| **Devices** | See device status, locate a device on the desk, adjust its screen size, include/exclude it from sharing, or forget a remote device. |
| **Settings** | Change clipboard and incoming-file permissions, open received files, configure microphones, preview edge lights, or leave the desk. |
| **Hotkeys** | Read the return shortcut and mouse/keyboard navigation guidance. |
| **About** | See the app version, platform information, project links, and issue-reporting link. |

Controls that require a connection or selected device are disabled until those requirements are met. Microphone selection also depends on compatible receivers and incoming-audio permission.

### Input and layouts

- **Automatic input** lets any connected Windows PC's keyboard or mouse become the active source without a switching popup. A fixed source can be selected from **Input mode**.
- Press **Ctrl + Alt + Backspace** to return the pointer to the current source PC.
- Closing or minimizing Velixa keeps it running in the system tray. Double-click the tray icon to reopen it, or select **Quit** to stop it.
- Use the **Desks** menu to create, rename, switch, or delete layouts. Up to 12 layouts can share the same paired devices and coordinator.
- Select a card, then use **Device actions**, or right-click it, to adjust its size or include/exclude it from input sharing. Excluding a device preserves its pairing and does not put the physical device to sleep. **Forget device** removes a remote device from every saved layout and requires pairing again.
- Screen cards preserve their proportions and snap together without overlap. Multiple smaller screens can occupy different sections of a larger screen's edge.
- Gold marks the local device; remote device borders are purple. Use **+**, **−**, and **Fit** to adjust the desk view.

### Android layout

Android uses the same navy-and-purple theme and logo as Windows. Setup shows **Enable continuity**, followed by **Scan QR code**. Once paired, the desk shows numbered screens with matching device names: drag a screen to arrange it, or tap it for input-sharing and size options.

The microphone card appears after continuity is enabled and the device is paired. Starting microphone sharing requires a live connection; stopping an active service remains available. **Disconnect this device** asks for confirmation. **Controls & help** explains mouse mappings and the return shortcut.

Android 8–12L also shows **Keyboard setup** with controls to enable and select the Velixa keyboard. Your normal touchscreen input remains local.

### Clipboard and files

Text and image clipboard changes are shared between available Windows devices. Clipboard sharing and incoming files can be disabled in Settings. Clipboard payloads are limited to 16 MB.

To transfer a file, drop it onto the destination Windows screen card. Transfers arrive in **Downloads/Velixa**; open it from **Settings > Open received files**. A status panel shows progress, speed, and cancellation controls. Existing files are never overwritten, and completed content is verified with SHA-256.

The following are not currently supported:

- Folder transfers
- Android file or clipboard sharing
- Direct drag-and-drop between Explorer windows
- Dropping files onto empty desk space

## Microphone sharing

Velixa lists available inputs with their source device, for example **USB microphone · Model name (VOSTRO)** or **System microphone (Motorola)**. The list refreshes approximately every five seconds.

Hardware names come from the operating system, while device-type labels depend on driver information. Android combines built-in routes into one system microphone and excludes telephony and virtual routes. Distinct USB, headset, and Bluetooth inputs remain selectable when exposed by the operating system.

### Share a Windows microphone

1. On every receiving PC, open **Settings > Configure microphone**, enable incoming audio, and select a receiving output.
2. On the source PC, choose a microphone from the main microphone menu. The capture stream is sent to all compatible Windows receivers that accept it.
3. To select that microphone from another PC, enable **Allow paired PCs to request this PC's microphone** on the source. This permission lasts for the current app session.
4. Select **Microphone off** to stop sharing or receiving audio on that PC.

### Use an Android microphone on Windows

1. Open Velixa on Android and tap **Enable microphone sharing to PCs**.
2. Grant microphone permission. A foreground notification remains visible and includes a **Stop** action.
3. On a Windows PC with incoming audio enabled, choose the phone's input from the microphone menu.

The same Android stream can be received by other available Windows PCs. Android sends microphone audio only. It cannot play a Windows microphone into phone calls, emulate a Bluetooth headset, or replace Android's telephony microphone. USB and Bluetooth routing depends on the device and driver.

### Use shared audio in Teams, Zoom, or another app

Standard Windows applications cannot create a system-wide microphone endpoint. To expose Velixa's incoming audio as a microphone, install a virtual audio cable separately on each receiving PC:

1. In Velixa, select the cable's playback endpoint, such as **CABLE Input**, as the receiving output.
2. In the calling or recording app, select the cable's recording endpoint, such as **CABLE Output**, as its microphone.

[VB-CABLE](https://vb-audio.com/Cable/) is one option and is distributed under its own license. Velixa does not bundle a virtual audio driver. Selecting speakers or headphones as the receiving output plays the stream aloud. On the source PC, select the physical microphone directly in the calling app. Velixa streams audio and does not save it to disk.

## Privacy and networking

Velixa is designed for trusted local networks:

- Each paired device receives separate credentials.
- Windows protects credentials with DPAPI; Android uses Keystore-backed storage.
- TLS verifies the paired coordinator certificate, and the coordinator authenticates reconnecting devices.
- Pairing codes are short-lived and rate-limited; QR pairing tokens are single-use.
- Clipboard, file, and audio data travels over the paired encrypted connection.
- No account, cloud relay, advertising, or input logging is required.

Velixa uses **TCP 37128** and **UDP 37129** on the local network. Windows firewall rules permit the local subnet on Private networks. Guest Wi-Fi isolation and corporate firewalls may block discovery. Device names and IDs are visible during LAN discovery.

On Android, camera permission is used only to scan pairing QR codes. Microphone permission is used only while the user-enabled microphone service is active. Camera sharing is not included.

## Current limitations

- Android input is replayed through Accessibility and remains subject to Android's platform restrictions.
- Android 8 through 12L also requires the included Velixa keyboard for typing.
- Some Android devices require **Allow restricted settings** for sideloaded Accessibility services.
- Normal use does not require ADB, root, or Shizuku.
- Windows secure desktops, sign-in screens, and elevated applications cannot be controlled by this non-elevated app.
- All monitors attached to one Windows PC are treated as a single combined desktop.
- The coordinator must remain awake. Automatic coordinator migration is not yet implemented.
- Hardware and driver behavior varies. Automated validation cannot guarantee compatibility with every PC, phone, microphone, or calling app.

Version 0.5.4 passed Windows integration, sharing, and desktop checks; Android emulator pairing, reconnection, layout, scrolling, and microphone checks; and microphone-route and theme checks. For exact coverage and remaining physical-device acceptance, see [Testing](TESTING.md).

## Build from source

### Requirements

- Windows
- PowerShell 7
- .NET Framework 4.8
- Inno Setup 6
- JDK 17, with Java tools on `PATH`
- Android SDK platform 35 or later with current stable build-tools

Dependencies and their notices are included in `deps/`.

```powershell
# Build Windows and Android
./build.ps1 -AndroidSdk "C:/path/to/Android/Sdk"

# Build Windows only
./build.ps1 -WindowsOnly

# Run automated Windows QA
./tests/run-tests.ps1
```

Build artifacts are written to `dist/`. The build checks that the Android manifest, Windows assembly, and installer versions agree before packaging. Android displays its version from the installed package metadata.

Android signing keys are created or reused under `%LOCALAPPDATA%/Velixa/build-signing`, outside the repository, with the password protected by DPAPI. Keep this directory private and backed up if you maintain Android releases. An APK signed with a different key cannot upgrade the official APK in place.

Production settings are stored under `%LOCALAPPDATA%/Velixa`. Test harnesses require `TESTING` and an explicit, isolated `VELIXA_TEST_DATA` directory. Tests use different network ports and refuse the production settings path. Preview and self-test modes also use isolated storage.

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request, and review [TESTING.md](TESTING.md) for the current test strategy and acceptance status.

## License

Velixa is available under the [MIT License](LICENSE). You may use, modify, redistribute, and sell it, including commercially, provided the copyright and permission notice is retained. The software is provided without warranty.

Third-party dependencies keep their respective licenses, which are listed in [third-party notices](assets/THIRD-PARTY-NOTICES.txt). External audio drivers are not part of this project.

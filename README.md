# Velixa 0.2 Preview

Use one keyboard and mouse across Windows PCs and Android screens on your local network. No account, internet activation, or cloud relay is required.

## Install and pair

1. Install the Windows setup on each PC and the signed APK on Android.
2. On one PC, choose **Create my desk**, then **Add device**.
3. Another Windows PC finds the desk automatically. Enter the four-digit code shown on the first PC.
4. On Android, enable Velixa continuity in Accessibility, then tap **Scan QR code**. Scan the Android tab in the Windows pairing dialog.
5. Drag screens left, right, above, or below to match your desk. Move your pointer across an edge to switch.

Upgrade all devices together and pair them once again when moving from 0.1 to 0.2. Pairing has changed and the old shared code is no longer used.

## Everyday use

- Use **Change** beside the keyboard-and-mouse source to choose another connected Windows laptop.
- Optional automatic source switching follows deliberate physical keyboard or mouse-click activity on that laptop. Android remains receive-only.
- Offline devices keep their positions and appear dimmed. A returning Windows receiver asks whether to reconnect when its paired desk is reachable.
- Closing the Windows window leaves Velixa in the system tray. Quit from its tray menu to stop it. The installer offers startup at Windows sign-in.
- **Ctrl + Alt + Backspace** returns input to the current source laptop.
- Edge-light previews and leaving a desk are under **Settings**.
- Android keeps the monochrome icon; Windows uses the supplied mark on white.

The PC that created the desk coordinates traffic and must remain awake, including when another laptop is the input source. This preview does not migrate the coordinator automatically.

## Security

Windows pairing uses Bouncy Castle SRP-6a with the RFC 5054 2048-bit group and SHA-256, bound to the TLS certificate and device identities. Four-digit codes last two minutes, allow at most five attempts, and are invalidated after successful pairing. Android QR codes carry a single-use 256-bit token and a certificate fingerprint; the app verifies that fingerprint before sending authentication.

Successful pairing creates a distinct 256-bit credential for each device. Reconnection verifies the saved certificate and device-bound proofs. Windows stores credentials with DPAPI; Android uses Keystore-backed AES-GCM. Input is accepted only from the selected Windows source and its current session epoch. Android cannot originate input. Network discovery exposes device names and IDs on the LAN. Input is not logged.

TCP 37128 carries TLS 1.2. UDP 37129 handles discovery. Windows firewall rules restrict inbound traffic to the local subnet on Private networks. Guest-network isolation or corporate firewall rules may block connections.

## Platform limits

Android 13+ typing uses Accessibility input connections. Android 8–12 also needs the included Velixa keyboard selected for typing. Android may require Allow restricted settings in App info for a sideloaded APK. No ADB, root, or Shizuku is used by the app.

Android gestures are replayed through Accessibility; they are not an unrestricted hardware mouse driver. Drags replay on release. Games, protected fields, and some shortcuts may behave differently. Android touch input never leaves Android. Windows secure desktops, login screens, and elevated applications are outside this non-elevated preview's scope. Windows monitors are treated as a combined desktop for switching between machines.

The Windows installer is not Authenticode signed. The APK is release-signed with the existing project signing identity. Manual multi-device acceptance remains separate from automated verification.

## Build

Run `build.ps1` with Java 17, Android SDK/build-tools, the Windows .NET Framework 4.8 compiler, and Inno Setup available. The application dependencies are bundled; installed apps download no packages. Signing material remains outside the source directory. GitHub contains only installers.

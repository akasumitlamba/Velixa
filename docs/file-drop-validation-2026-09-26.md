# File drop fix validation — 2026-09-26

- Added persistent file drop/progress panels to the bottom-right corner of every Windows monitor, independent of the main window.
- One connected Windows destination sends immediately; multiple destinations require choosing a named PC. Offline drops do not send.
- Panels accept OLE FileDrop and legacy WM_DROPFILES, and offer a file picker.
- Receiver progress begins at transfer acceptance. Real files named Clipboard* no longer disappear from progress.
- Concurrent transfers do not replace the currently displayed active transfer.
- Windows-only feature; direct Explorer-to-Explorer cross-device dragging and folders remain unsupported.

Validation:
- Windows installer and portable package built successfully.
- 8 new file drop regression checks passed, including simulated OLE drops, offline routing, monitor coordinates, progress selection and disposal.
- 24 sharing checks passed.
- An 8 MB file passed through two authenticated TLS clients and coordinator in 3.185 seconds, with content and heartbeat verification.
- 229 integration checks, 9 self-tests, 218 desktop QA checks, 42 continuity checks passed. Window regression completed 30 close/minimize/restore cycles.
- Progress-panel render visually inspected: build/file-transfer-progress.png.
- Full suite did not finish successfully: native touchpad test failed at 'test process receives foreground gestures'. No claim of physical two-PC drag verification.
- Existing installed app was not replaced; install the rebuilt package on both Windows PCs.

Installer: dist/Velixa-1.0.1-Windows-Setup.exe
SHA-256: 9049a83d9e2ebf345e0cf15470cac79cb6b9164b26bf72f3be8927f750fc1095
Detailed log: build/file-drop-validation.log

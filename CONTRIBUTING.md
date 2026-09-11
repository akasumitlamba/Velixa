# Contributing

Build with `build.ps1`, then run `tests/run-tests.ps1`. Add focused regression coverage for behavior changes. Keep the interface understandable without diagnostics or technical setup details on the main desk.

Never run network test fixtures against production ports or production settings. Use TESTING builds and isolated storage. Do not commit device data, pairing credentials, signing keys, logs, generated builds, or local machine paths.

Describe the problem, user-visible result, and validation in each pull request. Contributions are provided under the repository's MIT license.

param([string]$Serial="emulator-5580", [string]$AndroidSdk=$env:ANDROID_HOME)
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
Set-Location $project
if($Serial -notmatch '^emulator-') {throw 'Use a disposable emulator for instrumentation.'}
if(!$AndroidSdk){$AndroidSdk=Join-Path $env:LOCALAPPDATA 'Android/Sdk'}
$sdk=$AndroidSdk
$bt=(Get-ChildItem (Join-Path $sdk 'build-tools') -Directory | Where-Object Name -Match '^\d+\.\d+\.\d+$' | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1).FullName
$jar=Get-ChildItem (Join-Path $sdk 'platforms') -Directory | Sort-Object Name -Descending | ForEach-Object {Join-Path $_.FullName 'android.jar'} | Select-Object -First 1
New-Item -ItemType Directory -Force build/android-test/classes,build/android-test/dex | Out-Null
& "$bt/aapt2.exe" link -o build/android-test/base.apk -I $jar --manifest tests/android/AndroidManifest.xml
& javac -encoding UTF-8 -source 8 -target 8 -bootclasspath "$jar;$bt/core-lambda-stubs.jar" -classpath 'build/android/classes.jar;deps/zxing.jar' -d build/android-test/classes tests/android/Smoke.java
& jar cf build/android-test/classes.jar -C build/android-test/classes .
& "$bt/d8.bat" --lib $jar --classpath build/android/classes.jar --classpath deps/zxing.jar --min-api 26 --output build/android-test/dex build/android-test/classes.jar
Copy-Item build/android-test/base.apk build/android-test/unsigned.apk -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open((Join-Path $project 'build/android-test/unsigned.apk'),[IO.Compression.ZipArchiveMode]::Update)
[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,(Join-Path $project 'build/android-test/dex/classes.dex'),'classes.dex') | Out-Null
$zip.Dispose()
& "$bt/zipalign.exe" -f 4 build/android-test/unsigned.apk build/android-test/aligned.apk
Add-Type -AssemblyName System.Security
$signDir=Join-Path $env:LOCALAPPDATA 'Velixa/build-signing'
$env:VELIXA_SIGN_PASSWORD=[Text.Encoding]::UTF8.GetString([Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $signDir 'password.bin')),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser))
try{& "$bt/apksigner.bat" sign --ks (Join-Path $signDir 'android-release.keystore') --ks-key-alias velixa --ks-pass env:VELIXA_SIGN_PASSWORD --key-pass env:VELIXA_SIGN_PASSWORD --out build/android-test/test.apk build/android-test/aligned.apk}finally{Remove-Item Env:VELIXA_SIGN_PASSWORD}
& "$sdk/platform-tools/adb.exe" -s $Serial install -r build/android-test/test.apk
& "$sdk/platform-tools/adb.exe" -s $Serial push build/test-qr.png /data/local/tmp/velixa-qr.png
$result = & "$sdk/platform-tools/adb.exe" -s $Serial shell am instrument -w -e image /data/local/tmp/velixa-qr.png com.velixa.tests/com.velixa.app.Smoke
$result
if($LASTEXITCODE -ne 0 -or ($result -join "`n") -notmatch "13 Android checks passed") { throw "Android smoke checks did not pass" }

if(!(Test-Path build/android-mic-frames.txt) -or [int](Get-Content build/android-mic-frames.txt) -lt 8){throw 'Android microphone frames were not verified by the test coordinator'}

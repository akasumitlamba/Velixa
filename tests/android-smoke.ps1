param([string]$Serial="emulator-5580")
$ErrorActionPreference='Stop'
$project=Split-Path $PSScriptRoot -Parent
Set-Location $project
$env:PATH='C:/Program Files/Eclipse Adoptium/jdk-17.0.20.101-hotspot/bin;'+$env:PATH
$sdk='C:/Users/sumit/OneDrive/Documents/ChatGPT/AniBrowser/.tools/sdk'
$bt=Join-Path $sdk 'build-tools/37.0.0'
$jar=Join-Path $sdk 'platforms/android-37.1/android.jar'
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
if($LASTEXITCODE -ne 0 -or ($result -join "`n") -notmatch "6 Android checks passed") { throw "Android smoke checks did not pass" }

param([string]$AndroidSdk=$env:ANDROID_HOME)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if(!$AndroidSdk){throw 'Supply -AndroidSdk'}
$jar=Get-ChildItem (Join-Path $AndroidSdk 'platforms') -Directory | Sort-Object Name -Descending | ForEach-Object {Join-Path $_.FullName 'android.jar'} | Select-Object -First 1
New-Item -ItemType Directory -Force build/route-tests | Out-Null
& javac -encoding UTF-8 -classpath $jar -d build/route-tests android/src/com/velixa/app/MicrophoneRoutes.java tests/android/MicrophoneRoutesTest.java
if($LASTEXITCODE -ne 0){throw 'Route test compilation failed'}
& java -classpath build/route-tests com.velixa.app.MicrophoneRoutesTest
if($LASTEXITCODE -ne 0){throw 'Route tests failed'}

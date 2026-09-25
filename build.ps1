param([string]$AndroidSdk = $env:ANDROID_HOME, [switch]$WindowsOnly)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
Set-Location -LiteralPath $project
# Fail before packaging when platform or installer versions drift.
[xml]$releaseManifest=Get-Content android/AndroidManifest.xml
$releaseVersion=$releaseManifest.manifest.GetAttribute('versionName','http://schemas.android.com/apk/res/android')
$assemblyVersion=[regex]::Match((Get-Content windows/AssemblyInfo.cs -Raw),'AssemblyFileVersion\("([^" ]+)"\)').Groups[1].Value
$installerVersion=[regex]::Match((Get-Content windows/installer.iss -Raw),'(?m)^AppVersion=([^\r\n]+)').Groups[1].Value
if($assemblyVersion -ne "$releaseVersion.0" -or $installerVersion -ne $releaseVersion){throw 'Windows, Android and installer versions must match.'}

if (!$AndroidSdk) { $AndroidSdk = Join-Path $env:LOCALAPPDATA 'Android/Sdk' }
if (!$WindowsOnly) {
 $platform = Get-ChildItem (Join-Path $AndroidSdk 'platforms') -Directory | Where-Object Name -Match '^android-\d+(\.\d+)?$' | Sort-Object { [version](($_.Name -replace '^android-','')+'.0') } -Descending | ForEach-Object {Join-Path $_.FullName 'android.jar'} | Select-Object -First 1
 $androidTools = (Get-ChildItem (Join-Path $AndroidSdk 'build-tools') -Directory | Where-Object Name -Match '^\d+\.\d+\.\d+$' | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1).FullName
 if (!$platform -or !$androidTools) {throw 'Install Android SDK platform 35 or later and build-tools; pass -AndroidSdk or set ANDROID_HOME.'}
}
function Run([string]$exe, [string[]]$arguments) { & $exe @arguments; if ($LASTEXITCODE -ne 0) { throw "$exe failed: $LASTEXITCODE" } }
New-Item -ItemType Directory -Force build/windows,build/android/classes,dist,android/res/mipmap-mdpi,android/res/mipmap-anydpi-v26,android/res/mipmap-anydpi-v33 | Out-Null
& (Join-Path $project 'assets/build-icons.ps1')
Copy-Item deps/bc/lib/net461/BouncyCastle.Cryptography.dll build/windows/
Copy-Item deps/qr/lib/net40/QRCoder.dll build/windows/
$sources = Get-ChildItem windows -Filter *.cs | Select-Object -ExpandProperty FullName
Run (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') (@('/nologo','/target:winexe','/optimize+','/out:build/windows/Velixa.exe','/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Web.Extensions.dll','/r:System.Security.dll','/r:System.Core.dll','/r:build/windows/BouncyCastle.Cryptography.dll','/r:build/windows/QRCoder.dll','/win32manifest:windows/app.manifest','/win32icon:build/windows/velixa.ico') + $sources)
& (Join-Path $project 'tests/build-touchpad.ps1')
Copy-Item LICENSE build/windows/LICENSE.txt
Copy-Item windows/Velixa.exe.config build/windows/Velixa.exe.config
Copy-Item assets/THIRD-PARTY-NOTICES.txt build/windows/THIRD-PARTY-NOTICES.txt
if (!$WindowsOnly) {
Run (Join-Path $androidTools 'aapt2.exe') @('compile','--dir','android/res','-o','build/android/resources.zip')
New-Item -ItemType Directory -Force build/android/generated | Out-Null
Run (Join-Path $androidTools 'aapt2.exe') @('link','-o','build/android/base.apk','-I',$platform,'--manifest','android/AndroidManifest.xml','-A','android/assets','--java','build/android/generated','build/android/resources.zip','--auto-add-overlay')
$javaSources = @(Get-ChildItem android/src,build/android/generated -Recurse -Filter *.java | Select-Object -ExpandProperty FullName)
Run 'javac' (@('-encoding','UTF-8','-source','8','-target','8','-bootclasspath',($platform + ';' + (Join-Path $androidTools 'core-lambda-stubs.jar')),'-classpath','deps/zxing.jar','-d','build/android/classes') + $javaSources)
Run 'jar' @('cf','build/android/classes.jar','-C','build/android/classes','.')
New-Item -ItemType Directory -Force build/android/dex | Out-Null
Run (Join-Path $androidTools 'd8.bat') @('--lib',$platform,'--min-api','26','--output','build/android/dex','build/android/classes.jar','deps/zxing.jar')
Copy-Item build/android/base.apk build/android/unaligned.apk
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open((Join-Path $project 'build/android/unaligned.apk'),[IO.Compression.ZipArchiveMode]::Update)
foreach ($dex in Get-ChildItem build/android/dex -Filter *.dex) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$dex.FullName,$dex.Name) | Out-Null }
$zip.Dispose()
Run (Join-Path $androidTools 'zipalign.exe') @('-f','4','build/android/unaligned.apk','build/android/aligned.apk')
# Signing identity is local and outside the repository. Reuse it for upgrades.
$signDir = Join-Path $env:LOCALAPPDATA 'Velixa/build-signing'
New-Item -ItemType Directory -Force $signDir | Out-Null
$keyPath = Join-Path $signDir 'android-release.keystore'
$passwordPath = Join-Path $signDir 'password.bin'
Add-Type -AssemblyName System.Security
if (!(Test-Path -LiteralPath $keyPath)) {
 $random = New-Object byte[] 32
 $rng = [Security.Cryptography.RandomNumberGenerator]::Create();$rng.GetBytes($random);$rng.Dispose()
 $env:VELIXA_SIGN_PASSWORD = [Convert]::ToBase64String($random)
 [IO.File]::WriteAllBytes($passwordPath,[Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes($env:VELIXA_SIGN_PASSWORD),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser))
 Run 'keytool' @('-genkeypair','-keystore',$keyPath,'-storepass:env','VELIXA_SIGN_PASSWORD','-keypass:env','VELIXA_SIGN_PASSWORD','-alias','velixa','-keyalg','RSA','-keysize','3072','-validity','10000','-dname','CN=Velixa Local Release','-noprompt')
} else { $env:VELIXA_SIGN_PASSWORD = [Text.Encoding]::UTF8.GetString([Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($passwordPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)) }
try { Run (Join-Path $androidTools 'apksigner.bat') @('sign','--ks',$keyPath,'--ks-key-alias','velixa','--ks-pass','env:VELIXA_SIGN_PASSWORD','--key-pass','env:VELIXA_SIGN_PASSWORD','--out',"dist/Velixa-$releaseVersion-Android.apk",'build/android/aligned.apk') } finally { Remove-Item Env:VELIXA_SIGN_PASSWORD }
Run (Join-Path $androidTools 'apksigner.bat') @('verify','--verbose',"dist/Velixa-$releaseVersion-Android.apk")
}
Run 'C:/Program Files (x86)/Inno Setup 6/ISCC.exe' @('windows/installer.iss')
Compress-Archive -Path build/windows/LICENSE.txt,build/windows/Velixa.exe,build/windows/Velixa.exe.config,build/windows/velixa.ico,build/windows/velixa-logo.png,build/windows/BouncyCastle.Cryptography.dll,build/windows/QRCoder.dll,build/windows/Velixa.Touchpad.dll,build/windows/THIRD-PARTY-NOTICES.txt -DestinationPath dist/Velixa-$releaseVersion-Windows-Portable.zip -Force
$artifacts=@("dist/Velixa-$releaseVersion-Windows-Setup.exe","dist/Velixa-$releaseVersion-Windows-Portable.zip"); if (!$WindowsOnly) {$artifacts+="dist/Velixa-$releaseVersion-Android.apk"}
Get-Item $artifacts | Get-FileHash -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLower())  $([IO.Path]::GetFileName($_.Path))" } | Set-Content dist/SHA256SUMS-$releaseVersion.txt

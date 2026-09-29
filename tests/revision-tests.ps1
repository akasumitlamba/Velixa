$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
foreach($harness in @('DeskRevisionRegression','RecoveryRegression')) {
 $env:VELIXA_TEST_DATA=Join-Path $PWD ('build/revision-'+[guid]::NewGuid().ToString())
 try {
  $sources=@('windows/Protocol.cs','windows/Input.cs','windows/Desk.cs',('tests/'+$harness+'.cs')) | ForEach-Object {(Resolve-Path $_).Path}
  & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING ('/out:build/windows/'+$harness+'.exe') /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll @sources
  if($LASTEXITCODE -ne 0){throw "$harness compilation failed"}
  & ('./build/windows/'+$harness+'.exe')
  if($LASTEXITCODE -ne 0){throw "$harness failed"}
 } finally {Remove-Item Env:VELIXA_TEST_DATA}
}
New-Item -ItemType Directory -Force build/srp | Out-Null
& javac -d build/srp android/src/com/velixa/app/SrpClient.java tests/SrpInterop.java
if($LASTEXITCODE -ne 0){throw 'Android SRP interop compilation failed'}
& 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /out:build/windows/SrpInterop.exe /r:build/windows/BouncyCastle.Cryptography.dll (Resolve-Path tests/SrpInterop.cs).Path
if($LASTEXITCODE -ne 0){throw 'Windows SRP interop compilation failed'}
& ./build/windows/SrpInterop.exe
if($LASTEXITCODE -ne 0){throw 'Android/Windows SRP interop failed'}

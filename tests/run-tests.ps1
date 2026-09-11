$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
$env:VELIXA_TEST_DATA=Join-Path $PWD ('build/test-data-'+[guid]::NewGuid().ToString())
$sources=@('windows/Protocol.cs','windows/Input.cs','windows/Desk.cs','tests/Integration.cs') | ForEach-Object {(Resolve-Path $_).Path}
& 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING /out:build/windows/Integration.exe /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:build/windows/BouncyCastle.Cryptography.dll @sources
if ($LASTEXITCODE -ne 0) {throw 'Harness compilation failed'}
& ./build/windows/Integration.exe
if ($LASTEXITCODE -ne 0) {throw 'Integration tests failed'}
Remove-Item Env:VELIXA_TEST_DATA
$test=Start-Process -FilePath (Resolve-Path build/windows/Velixa.exe) -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -Wait
if ($test.ExitCode -ne 0) {throw 'Windows self-tests failed'}
Get-Content build/windows/self-test.txt

$env:VELIXA_TEST_DATA=Join-Path $PWD ('build/window-tests-'+[guid]::NewGuid().ToString())
try {
 $windowSources=@(Get-ChildItem windows -Filter *.cs | ForEach-Object FullName)+(Resolve-Path tests/WindowRegression.cs).Path
 & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING /main:WindowRegression /out:build/windows/WindowRegression.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll /r:build/windows/QRCoder.dll @windowSources
 if ($LASTEXITCODE -ne 0) {throw 'Window regression compilation failed'}
 & ./build/windows/WindowRegression.exe
 if ($LASTEXITCODE -ne 0) {throw 'Minimize regression failed'}
} finally { Remove-Item Env:VELIXA_TEST_DATA }

$env:VELIXA_TEST_DATA=Join-Path $PWD ('build/sharing-tests-'+[guid]::NewGuid().ToString())
try {
 $sharingSources=@('windows/Protocol.cs','windows/Sharing.cs','windows/Microphone.cs','tests/SharingRegression.cs') | ForEach-Object {(Resolve-Path $_).Path}
 & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING /main:SharingRegression /out:build/windows/SharingRegression.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll @sharingSources
 if ($LASTEXITCODE -ne 0) {throw 'Sharing regression compilation failed'}
 & ./build/windows/SharingRegression.exe
 if ($LASTEXITCODE -ne 0) {throw 'Sharing regression failed'}
} finally { Remove-Item Env:VELIXA_TEST_DATA }

$env:VELIXA_TEST_DATA=Join-Path $PWD ('build/network-sharing-'+[guid]::NewGuid().ToString())
try {
 $networkSources=@('windows/Protocol.cs','windows/Input.cs','windows/Desk.cs','windows/Sharing.cs','tests/SharingNetwork.cs') | ForEach-Object {(Resolve-Path $_).Path}
 & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING /main:SharingNetwork /out:build/windows/SharingNetwork.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll @networkSources
 if ($LASTEXITCODE -ne 0) {throw 'Network sharing harness compilation failed'}
 & ./build/windows/SharingNetwork.exe
 if ($LASTEXITCODE -ne 0) {throw 'Encrypted sharing regression failed'}
} finally { Remove-Item Env:VELIXA_TEST_DATA }

foreach($harness in @('CaptionClickRegression','DialogRegression')) {
 $env:VELIXA_TEST_DATA=Join-Path $PWD ('build/ui-tests-'+[guid]::NewGuid().ToString())
 try {
  $uiSources=@(Get-ChildItem windows -Filter *.cs | ForEach-Object FullName)+(Resolve-Path ('tests/'+$harness+'.cs')).Path
  & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING ('/main:'+$harness) ('/out:build/windows/'+$harness+'.exe') /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll /r:build/windows/QRCoder.dll @uiSources
  if ($LASTEXITCODE -ne 0) {throw "$harness compilation failed"}
  & ('./build/windows/'+$harness+'.exe')
  if ($LASTEXITCODE -ne 0) {throw "$harness failed"}
 } finally { Remove-Item Env:VELIXA_TEST_DATA }
}

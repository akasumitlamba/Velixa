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

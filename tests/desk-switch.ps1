$ErrorActionPreference='Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$env:VELIXA_TEST_DATA=Join-Path $PWD ('build/desk-switch-'+[guid]::NewGuid())
try {
 $sources=@(Get-ChildItem windows -Filter *.cs | ForEach-Object FullName)+(Resolve-Path tests/DeskSwitchRegression.cs).Path
 & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING /main:DeskSwitchRegression /out:build/windows/DeskSwitchRegression.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll /r:build/windows/QRCoder.dll /r:build/windows/NAudio.dll @sources
 if($LASTEXITCODE -ne 0){throw 'Desk switching test compilation failed'}
 & ./build/windows/DeskSwitchRegression.exe
 if($LASTEXITCODE -ne 0){throw 'Desk switching test failed'}
} finally {Remove-Item Env:VELIXA_TEST_DATA}

$ErrorActionPreference='Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$sources=@(Get-ChildItem windows -Filter *.cs | ForEach-Object FullName)+(Resolve-Path tests/UpdaterRegression.cs).Path
& 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /main:UpdaterRegression /out:build/windows/UpdaterRegression.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll /r:build/windows/QRCoder.dll /r:build/windows/NAudio.dll @sources
if($LASTEXITCODE -ne 0){throw 'Updater test compilation failed'}
& ./build/windows/UpdaterRegression.exe
if($LASTEXITCODE -ne 0){throw 'Updater tests failed'}

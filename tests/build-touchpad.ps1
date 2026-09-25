param([string]$Output = (Join-Path $PSScriptRoot '../build/windows/Velixa.Touchpad.dll'))
$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$metadata=Join-Path $env:WINDIR 'System32/WinMetadata'
$runtime=Get-ChildItem (Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL/System.Runtime') -Filter System.Runtime.dll -Recurse | Select-Object -First 1 -ExpandProperty FullName
$refs=@('Windows.UI','Windows.Foundation','Windows.Devices') | ForEach-Object { '/r:'+(Join-Path $metadata ($_+'.winmd')) }
& (Join-Path $framework 'csc.exe') /nologo /target:library /optimize+ ('/out:'+$Output) ('/r:'+$runtime) ('/r:'+(Join-Path $framework 'System.Runtime.WindowsRuntime.dll')) ('/r:'+(Join-Path $framework 'System.Runtime.InteropServices.WindowsRuntime.dll')) @refs (Join-Path $PSScriptRoot '../windows/touchpad/TouchpadBridge.cs')
if($LASTEXITCODE -ne 0){throw 'Touchpad bridge compilation failed. Build on an updated Windows 11 with Precision Touchpad API metadata.'}

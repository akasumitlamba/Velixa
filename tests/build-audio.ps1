param([string]$Output = (Join-Path $PSScriptRoot '../build/windows/Velixa.Audio.dll'))
$ErrorActionPreference='Stop'
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$vs=& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if(!$vs){throw 'Install Visual Studio C++ build tools and Windows SDK for the audio bridge.'}
$vc=(Get-ChildItem (Join-Path $vs 'VC/Tools/MSVC') -Directory | Sort-Object Name -Descending | Select-Object -First 1).FullName
$sdk=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10'
$version=(Get-ChildItem (Join-Path $sdk 'Include') -Directory | Sort-Object Name -Descending | Select-Object -First 1).Name
$includes=@('/I'+(Join-Path $vc 'include'))
foreach($part in @('ucrt','shared','um','winrt')){$includes+=('/I'+(Join-Path $sdk "Include/$version/$part"))}
$libraries=@('/LIBPATH:'+(Join-Path $vc 'lib/x64'))
foreach($part in @('ucrt','um')){$libraries+=('/LIBPATH:'+(Join-Path $sdk "Lib/$version/$part/x64"))}
& (Join-Path $vc 'bin/Hostx64/x64/cl.exe') /nologo /std:c++17 /EHsc /O2 /MT /LD @includes (Join-Path $PSScriptRoot '../windows/audio/ProcessLoopback.cpp') ('/Fo'+(Join-Path (Split-Path $Output) 'ProcessLoopback.obj')) /link @libraries ole32.lib mmdevapi.lib ('/OUT:'+$Output) ('/IMPLIB:'+[IO.Path]::ChangeExtension($Output,'.lib'))
if($LASTEXITCODE -ne 0){throw 'Audio bridge compilation failed'}


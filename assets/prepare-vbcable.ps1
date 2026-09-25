$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$cache=Join-Path $root 'build/audio-driver'
$archive=Join-Path $cache 'VBCABLE_Driver_Pack45.zip'
$expected='B950E39F01AF1D04EA623C8F6D8EB9B6EA5C477C637295FABF20631C85116BFB'
New-Item -ItemType Directory -Force $cache | Out-Null
if(!(Test-Path $archive)){Invoke-WebRequest 'https://download.vb-audio.com/Download_CABLE/VBCABLE_Driver_Pack45.zip' -OutFile $archive}
if((Get-FileHash $archive -Algorithm SHA256).Hash -ne $expected){throw 'VB-CABLE archive does not match the reviewed package'}
$destination=Join-Path $root 'build/windows/VB-CABLE'
New-Item -ItemType Directory -Force $destination | Out-Null
Expand-Archive -LiteralPath $archive -DestinationPath $destination -Force
foreach($file in @('VBCABLE_Setup_x64.exe','vbaudio_cable64_win10.cat')){if((Get-AuthenticodeSignature (Join-Path $destination $file)).Status -ne 'Valid'){throw ('Invalid VB-CABLE signature: '+$file)}}
Copy-Item $archive $destination
Copy-Item (Join-Path $root 'assets/VB-CABLE-NOTICE.txt') $destination

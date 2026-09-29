$ErrorActionPreference='Stop'
Set-Location (Join-Path $PSScriptRoot '..')
& ./tests/build-audio.ps1
& 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /out:build/windows/AudioLoopbackRegression.exe /r:build/windows/NAudio.dll (Resolve-Path windows/ProcessAudio.cs).Path (Resolve-Path tests/AudioLoopbackRegression.cs).Path
if($LASTEXITCODE -ne 0){throw 'Audio capture test compilation failed'}
$signal=Join-Path $PWD ('build/audio-ready-'+[guid]::NewGuid()+'.txt')
$tone=Start-Process (Join-Path $PWD 'build/windows/AudioLoopbackRegression.exe') -ArgumentList @('tone',('"'+$signal+'"')) -WindowStyle Hidden -PassThru
try{& ./build/windows/AudioLoopbackRegression.exe capture $signal;if($LASTEXITCODE -ne 0){throw 'Audio capture regression failed'}}finally{if(!$tone.HasExited){$tone.WaitForExit(10000)|Out-Null};if(Test-Path -LiteralPath $signal){Remove-Item -LiteralPath $signal}}


$env:VELIXA_TEST_DATA=Join-Path $PWD ('build/audio-policy-'+[guid]::NewGuid())
try {
 $sources=@('windows/Protocol.cs','windows/Input.cs','windows/Desk.cs','windows/AudioEndpoints.cs','tests/AudioPolicyRegression.cs') | ForEach-Object {(Resolve-Path $_).Path}
 & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING,AUDIO_POLICY_TEST /main:AudioPolicyRegression /out:build/windows/AudioPolicyRegression.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll /r:build/windows/NAudio.dll @sources
 if($LASTEXITCODE -ne 0){throw 'Audio policy regression compilation failed'}
 & ./build/windows/AudioPolicyRegression.exe
 if($LASTEXITCODE -ne 0){throw 'Audio policy regression failed'}
} finally {Remove-Item Env:VELIXA_TEST_DATA}

$env:VELIXA_TEST_DATA=Join-Path $PWD ('build/speaker-route-'+[guid]::NewGuid())
try {
 $sources=@(Get-ChildItem windows -Filter *.cs | ForEach-Object FullName)+(Resolve-Path tests/SpeakerRouteRegression.cs).Path
 & 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe' /nologo /define:TESTING /main:SpeakerRouteRegression /out:build/windows/SpeakerRouteRegression.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Core.dll /r:build/windows/BouncyCastle.Cryptography.dll /r:build/windows/QRCoder.dll /r:build/windows/NAudio.dll @sources
 if($LASTEXITCODE -ne 0){throw 'Speaker route regression compilation failed'}
 & ./build/windows/SpeakerRouteRegression.exe
 if($LASTEXITCODE -ne 0){throw 'Speaker route regression failed'}
} finally {Remove-Item Env:VELIXA_TEST_DATA}

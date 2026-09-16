param([switch]$Clean)
. "$PSScriptRoot/Common.ps1"
$nativeLock = Get-Content -LiteralPath (Join-Path $ProjectRoot 'native/dependencies.lock.json') -Raw | ConvertFrom-Json
foreach ($item in $nativeLock.files) { if ((Get-FileHash -LiteralPath (Join-Path $ProjectRoot ('native/win-x64/' + $item.file))).Hash -ne $item.sha256) { throw "Native dependency hash mismatch: $($item.file)" } }
$solution = Join-Path $ProjectRoot 'ProximityAndGlobalVoiceChat.sln'
if ($Clean) { Invoke-Dotnet @('clean', $solution, '-c', 'Release', '--nologo', '-v', 'quiet') }
Invoke-Dotnet @('build', $solution, '-c', 'Release', '--nologo', '-p:RestoreLockedMode=true', '-v', 'minimal')

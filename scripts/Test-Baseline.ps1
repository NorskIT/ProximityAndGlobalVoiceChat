param()
. "$PSScriptRoot/Common.ps1"
& "$PSScriptRoot/Build.ps1"
Build-Verifier
Invoke-Dotnet @($Verifier, 'identity', (Join-Path $ProjectRoot "src/$ModName/bin/Release/net481/$ModName.dll"))
Invoke-Dotnet @('run', '--project', (Join-Path $ProjectRoot 'tools/TestAudio'), '-c', 'Release')
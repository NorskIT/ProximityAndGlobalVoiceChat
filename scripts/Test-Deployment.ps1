param()
. "$PSScriptRoot/Common.ps1"
$sandbox = Join-Path $ProjectRoot ('.cache/dt-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$testProfile = Join-Path $sandbox 'Development/BepInEx'
New-Item -ItemType Directory -Force (Join-Path $testProfile 'core'), (Join-Path $testProfile 'plugins'), (Join-Path $testProfile 'config') | Out-Null
Copy-Item -LiteralPath "$env:APPDATA/com.kesomannen.gale/valheim/profiles/Development/BepInEx/core/BepInEx.dll" -Destination (Join-Path $testProfile 'core')
$sentinel = Join-Path $testProfile 'plugins/unrelated.txt'
$config = Join-Path $testProfile 'config/Azumatt.ProximityVoiceChat.cfg'
Set-Content -LiteralPath $sentinel -Value 'preserve unrelated mod'
Set-Content -LiteralPath $config -Value 'preserve old settings'
$sentinelHash = (Get-FileHash -LiteralPath $sentinel).Hash
$configHash = (Get-FileHash -LiteralPath $config).Hash
$destination = Join-Path $testProfile "plugins/$ModName"

& "$PSScriptRoot/Deploy-Local.ps1" -BepInExPath $testProfile
if (!(Test-Path -LiteralPath (Join-Path $destination "$ModName.dll"))) { throw 'First installation failed.' }
Set-Content -LiteralPath (Join-Path $destination 'previous-marker.txt') -Value 'previous installation'
& "$PSScriptRoot/Deploy-Local.ps1" -BepInExPath $testProfile
if (Test-Path -LiteralPath (Join-Path $destination 'previous-marker.txt')) { throw 'Old files leaked into new installation.' }
& "$PSScriptRoot/Rollback-Local.ps1" -BepInExPath $testProfile
if (!(Test-Path -LiteralPath (Join-Path $destination 'previous-marker.txt'))) { throw 'Rollback did not restore previous installation.' }
& "$PSScriptRoot/Rollback-Local.ps1" -BepInExPath $testProfile
if (Test-Path -LiteralPath $destination) { throw 'First-install rollback did not remove the fork.' }

$original = "$env:APPDATA/com.kesomannen.gale/cache/Azumatt-ProximityVoiceChat/1.0.2/BepInEx/plugins/Azumatt-ProximityVoiceChat/ProximityVoiceChat.dll"
Copy-Item -LiteralPath $original -Destination (Join-Path $testProfile 'plugins/disguised-original.dll')
$blocked = $false
try { & "$PSScriptRoot/Deploy-Local.ps1" -BepInExPath $testProfile } catch {
    if ($_.Exception.Message -notlike 'Conflicting voice plugin:*') { throw }
    $blocked = $true
}
if (!$blocked -or (Test-Path -LiteralPath $destination)) { throw 'Original-plugin conflict was not safely blocked.' }
$blocked = $false
try { Get-DevelopmentPath (Join-Path $sandbox 'Prod-v1/BepInEx') } catch { $blocked = $true }
if (!$blocked) { throw 'Non-development profile was accepted.' }
if ((Get-FileHash -LiteralPath $sentinel).Hash -ne $sentinelHash -or (Get-FileHash -LiteralPath $config).Hash -ne $configHash) { throw 'Unrelated files changed.' }
Write-Output 'PASS: fresh install, replacement, both rollback cases, original GUID conflict, wrong profile rejection, unrelated files preserved.'

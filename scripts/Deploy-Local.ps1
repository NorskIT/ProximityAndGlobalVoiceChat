param(
    [string]$BepInExPath = "$env:APPDATA/com.kesomannen.gale/valheim/profiles/Development/BepInEx"
)
. "$PSScriptRoot/Common.ps1"
$profile = Get-DevelopmentPath $BepInExPath
$plugins = Join-Path $profile 'plugins'
$destination = Join-Path $plugins $ModName
Assert-ChildPath $destination $profile
if (Test-Path -LiteralPath (Join-Path $destination 'manifest.json')) {
    throw 'This fork is managed by Gale. Use scripts/Import-Gale.ps1 to update it through Gale.'
}
Build-Verifier
$existing = @(Get-VoicePlugins $plugins)
$expectedDll = Join-Path $destination "$ModName.dll"
foreach ($file in $existing) {
    if ($file -ne $expectedDll) { throw "Conflicting voice plugin: $file. Disable/remove it in Gale before deploying." }
}
& "$PSScriptRoot/Build-Package.ps1"
$zip = Join-Path $ProjectRoot "artifacts/NorskIT-$ModName-$ModVersion.zip"
$backupRoot = Join-Path (Split-Path $profile -Parent) '.pagvc-backups'
$transaction = Join-Path $backupRoot ((Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
Assert-ChildPath $transaction (Split-Path $profile -Parent)
New-Item -ItemType Directory -Force $transaction | Out-Null
$extracted = Join-Path $ProjectRoot ('.cache/unpack-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
Expand-Archive -LiteralPath $zip -DestinationPath $extracted
$hadPrevious = Test-Path -LiteralPath $destination
@{ hadPrevious = $hadPrevious; version = $ModVersion; destination = $destination } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $transaction 'receipt.json') -Encoding UTF8
New-Item -ItemType Directory -Force $plugins | Out-Null
$installedNew = $false
$movedPrevious = $false
$null = Get-DevelopmentPath $BepInExPath
try {
    if ($hadPrevious) {
        Move-Item -LiteralPath $destination -Destination (Join-Path $transaction 'previous')
        $movedPrevious = $true
    }
    Move-Item -LiteralPath (Join-Path $extracted "BepInEx/plugins/$ModName") -Destination $destination
    $installedNew = $true
    $installed = @(Get-VoicePlugins $plugins)
    if ($installed.Count -ne 1 -or $installed[0] -ne $expectedDll) { throw 'Expected exactly one installed fork and no original plugin.' }
    $built = Join-Path $ProjectRoot "src/$ModName/bin/Release/net481/$ModName.dll"
    if ((Get-FileHash -LiteralPath $expectedDll).Hash -ne (Get-FileHash -LiteralPath $built).Hash) { throw 'Installed DLL hash differs from build.' }
    Invoke-Dotnet @($Verifier, 'identity', $expectedDll)
} catch {
    if ($installedNew) { Move-Item -LiteralPath $destination -Destination (Join-Path $transaction 'failed') }
    if ($movedPrevious) { Move-Item -LiteralPath (Join-Path $transaction 'previous') -Destination $destination }
    Set-Content -LiteralPath (Join-Path $transaction 'failed.txt') -Value $_.Exception.Message
    throw
}
Set-Content -LiteralPath (Join-Path $transaction 'completed.txt') -Value (Get-Date -Format o)
Write-Output "Installed $ModName $ModVersion in $destination"
Write-Output "Verified one active voice plugin and matching SHA-256. Rollback saved in $transaction"
Write-Output 'Ready to start Valheim from Gale > Development.'

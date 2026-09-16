param([string]$BepInExPath = "$env:APPDATA/com.kesomannen.gale/valheim/profiles/Development/BepInEx")
. "$PSScriptRoot/Common.ps1"
$profile = Get-DevelopmentPath $BepInExPath
$destination = Join-Path $profile "plugins/$ModName"
$backupRoot = Join-Path (Split-Path $profile -Parent) '.pagvc-backups'
Assert-ChildPath $destination $profile
Assert-ChildPath $backupRoot (Split-Path $profile -Parent)
$backup = Get-ChildItem -LiteralPath $backupRoot -Directory | Where-Object {
    (Test-Path -LiteralPath (Join-Path $_.FullName 'completed.txt')) -and !(Test-Path -LiteralPath (Join-Path $_.FullName 'rolled-back.txt'))
} | Sort-Object Name -Descending | Select-Object -First 1
if (!$backup) { throw 'No completed deployment available to roll back.' }
Assert-ChildPath $backup.FullName $backupRoot
$receipt = Get-Content -LiteralPath (Join-Path $backup.FullName 'receipt.json') -Raw | ConvertFrom-Json
if ($receipt.destination -ne $destination) { throw 'Backup belongs to a different destination.' }
$previous = Join-Path $backup.FullName 'previous'
Assert-ChildPath $previous $backupRoot
if ($receipt.hadPrevious -and !(Test-Path -LiteralPath $previous)) { throw 'Previous installation is missing from the backup.' }
$removed = Join-Path $backup.FullName 'removed'
if (Test-Path -LiteralPath $destination) { Move-Item -LiteralPath $destination -Destination $removed }
try {
    if ($receipt.hadPrevious) { Move-Item -LiteralPath $previous -Destination $destination }
} catch {
    if (Test-Path -LiteralPath $removed) { Move-Item -LiteralPath $removed -Destination $destination }
    throw
}
Set-Content -LiteralPath (Join-Path $backup.FullName 'rolled-back.txt') -Value (Get-Date -Format o)
Write-Output 'Rolled back the last fork deployment. Configuration was preserved.'

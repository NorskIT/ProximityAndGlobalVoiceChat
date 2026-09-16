param([string]$GaleExe = "$env:ProgramFiles/Gale/gale.exe")
. "$PSScriptRoot/Common.ps1"
$profile = Get-DevelopmentPath "$env:APPDATA/com.kesomannen.gale/valheim/profiles/Development/BepInEx"
if (!(Test-Path -LiteralPath $GaleExe)) { throw 'Specify the Gale executable with -GaleExe.' }
$manualDll = Join-Path $profile "plugins/$ModName/$ModName.dll"
if (Test-Path -LiteralPath $manualDll) { throw 'A manual installation exists. Back it up outside plugins before importing to avoid a duplicate DLL.' }
& "$PSScriptRoot/Build-Package.ps1"
$zip = Join-Path $ProjectRoot "artifacts/NorskIT-$ModName-$ModVersion.zip"
# Gale registers the local mod in its own database and refreshes the open UI.
# CLI forwarding is asynchronous when Gale is already running.
& $GaleExe --game valheim --profile Development --install $zip
if ($LASTEXITCODE -ne 0) { throw "Gale import command failed: $LASTEXITCODE" }
Write-Output "Import requested in Gale > Development: $ModName $ModVersion. Check the Gale mod list for completion."

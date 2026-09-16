param()
. "$PSScriptRoot/Common.ps1"
& "$PSScriptRoot/Build.ps1"
Build-Verifier
$settings = (Invoke-Dotnet @('msbuild', (Join-Path $ProjectRoot "src/$ModName/$ModName.csproj"), '-getProperty:VALHEIM_INSTALL,BEPINEX_PATH') | Out-String) | ConvertFrom-Json
Invoke-Dotnet @($Verifier, 'references', (Join-Path $ProjectRoot "src/$ModName/bin/Release/net481/$ModName.dll"), (Join-Path $settings.Properties.VALHEIM_INSTALL 'valheim_Data/Managed'), (Join-Path $settings.Properties.BEPINEX_PATH 'core'))
$artifacts = Join-Path $ProjectRoot 'artifacts'
$staging = Join-Path $artifacts ('package-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $staging "BepInEx/plugins/$ModName"
New-Item -ItemType Directory -Force $payload | Out-Null
Copy-Item -LiteralPath (Join-Path $ProjectRoot "src/$ModName/bin/Release/net481/$ModName.dll") -Destination $payload
Copy-Item -LiteralPath (Join-Path $ProjectRoot "src/$ModName/bin/Release/net481/Native") -Destination $payload -Recurse
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'README.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'packaging/manifest.json') -Destination $staging
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'packaging/icon.png') -Destination $staging
Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'licenses') -File | Copy-Item -Destination $payload
Copy-Item -LiteralPath (Join-Path $ProjectRoot 'upstream/PROVENANCE.md') -Destination $payload
$zip = Join-Path $artifacts "NorskIT-$ModName-$ModVersion.zip"
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
# Write portable forward-slash entry names for Gale and other ZIP readers.
$zipStream = [IO.File]::Open($zip, [IO.FileMode]::Create)
$archive = New-Object IO.Compression.ZipArchive($zipStream, [IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem -LiteralPath $staging -Recurse -File | ForEach-Object {
        $entryName = $_.FullName.Substring($staging.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entryName) | Out-Null
    }
} finally { $archive.Dispose(); $zipStream.Dispose() }
& "$PSScriptRoot/Validate-Package.ps1" -PackagePath $zip
Write-Output "Package: $zip"

param([Parameter(Mandatory)][string]$PackagePath)
. "$PSScriptRoot/Common.ps1"
Build-Verifier
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($PackagePath))
try {
    $entries = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName.Replace('\', '/') })
    $prefix = "BepInEx/plugins/$ModName/"
    $expected = @('manifest.json', 'README.md', 'icon.png', ($prefix + "$ModName.dll"), ($prefix + 'PROVENANCE.md'))
    $expected += @(($prefix + 'Native/pagvc_opus.dll'), ($prefix + 'Native/pagvc_rnnoise.dll'))
$expected += Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'licenses') -File | ForEach-Object { $prefix + $_.Name }
    if (Compare-Object ($expected | Sort-Object) ($entries | Sort-Object)) { throw 'Package contains missing or unexpected files.' }
    $reader = New-Object IO.StreamReader($archive.GetEntry('manifest.json').Open())
    try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($manifest.name -ne $ModName -or $manifest.version_number -ne $ModVersion) { throw 'Wrong package identity.' }
    $temp = Join-Path $ProjectRoot ('.cache/validate-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force $temp | Out-Null
    $dll = Join-Path $temp "$ModName.dll"
    $dllEntry = $archive.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq ($prefix + "$ModName.dll") }
    [IO.Compression.ZipFileExtensions]::ExtractToFile($dllEntry, $dll)
        $nativeLock = Get-Content -LiteralPath (Join-Path $ProjectRoot 'native/dependencies.lock.json') -Raw | ConvertFrom-Json
    foreach ($item in $nativeLock.files) {
        $file = Join-Path $temp $item.file
        [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry($prefix + 'Native/' + $item.file), $file)
        if ((Get-FileHash -LiteralPath $file).Hash -ne $item.sha256) { throw 'Packaged native hash mismatch.' }
    }
    Invoke-Dotnet @($Verifier, 'identity', $dll)
    $built = Join-Path $ProjectRoot "src/$ModName/bin/Release/net481/$ModName.dll"
    if ((Get-FileHash -LiteralPath $dll).Hash -ne (Get-FileHash -LiteralPath $built).Hash) { throw 'Package DLL differs from current Release build.' }
} finally { $archive.Dispose() }
Write-Output 'PASS: exact package contents, plugin identity, embedded resources and build hash.'

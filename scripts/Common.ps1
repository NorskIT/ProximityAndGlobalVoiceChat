$ErrorActionPreference = 'Stop'
$ProjectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$ModName = 'ProximityAndGlobalVoiceChat'
$ModVersion = '0.2.3'
$Verifier = Join-Path $ProjectRoot 'tools/VerifyFork/bin/Release/net10.0/VerifyFork.dll'

function Invoke-Dotnet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

function Build-Verifier {
    Invoke-Dotnet @('build', (Join-Path $ProjectRoot 'tools/VerifyFork/VerifyFork.csproj'), '-c', 'Release', '--nologo', '-p:RestoreLockedMode=true', '-v', 'quiet') | Out-Host
}

function Get-DevelopmentPath {
    param([string]$BepInExPath)
    $path = [IO.Path]::GetFullPath($BepInExPath).TrimEnd('\', '/')
    if ((Split-Path $path -Leaf) -ne 'BepInEx' -or (Split-Path (Split-Path $path -Parent) -Leaf) -ne 'Development') {
        throw 'Deployment is restricted to a Development/BepInEx directory.'
    }
    if (!(Test-Path -LiteralPath (Join-Path $path 'core/BepInEx.dll'))) { throw 'Development profile is missing BepInEx/core/BepInEx.dll.' }
    if (Get-Process -Name valheim -ErrorAction SilentlyContinue) { throw 'Close Valheim before changing the plugin installation.' }
    return $path
}

function Assert-ChildPath {
    param([string]$Path, [string]$Parent)
    $full = [IO.Path]::GetFullPath($Path)
    $prefix = [IO.Path]::GetFullPath($Parent).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (!$full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Path is outside intended directory: $full" }
    # Refuse junctions/symlinks along the path before directory moves/deletions.
    $cursor = $full
    while ($cursor -and $cursor.Length -ge $prefix.Length - 1) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Linked path is not supported: $cursor" }
        }
        $cursor = Split-Path $cursor -Parent
    }
}

function Get-VoicePlugins {
    param([string]$PluginsPath)
    if (Test-Path -LiteralPath $PluginsPath) { Invoke-Dotnet @($Verifier, 'conflicts', $PluginsPath) }
}

param()
. "$PSScriptRoot/Common.ps1"
$zig = Join-Path $ProjectRoot '.tools/zig-x86_64-windows-0.14.1/zig.exe'
function Fetch-Checked($url, $path, $hash) {
    if (!(Test-Path -LiteralPath $path)) { Invoke-WebRequest -UseBasicParsing $url -OutFile $path }
    if ((Get-FileHash -LiteralPath $path).Hash -ne $hash) { throw "Hash mismatch: $path" }
}
New-Item -ItemType Directory -Force (Join-Path $ProjectRoot '.cache'), (Join-Path $ProjectRoot '.tools') | Out-Null
if (!(Test-Path -LiteralPath $zig)) {
    $zip = Join-Path $ProjectRoot '.cache/zig.zip'
    Fetch-Checked 'https://ziglang.org/download/0.14.1/zig-x86_64-windows-0.14.1.zip' $zip '554F5378228923FFD558EAC35E21AF020C73789D87AFEABF4BFD16F2E6FEED2C'
    Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $ProjectRoot '.tools') -Force
}
foreach ($repo in @(@('opus','v1.6.1','22244de5a79bd1d6d623c32e72bf1954b56235be'), @('rnnoise','v0.2','904a876dce1f9ab8860c0a5000ed151f9f6eef58'))) {
    $dir = Join-Path $ProjectRoot ('.cache/' + $repo[0])
    if (!(Test-Path -LiteralPath $dir)) { git clone --depth 1 --branch $repo[1] ("https://github.com/xiph/" + $repo[0] + '.git') $dir; if ($LASTEXITCODE) { throw 'Source download failed' } }
    if ((git -C $dir rev-parse HEAD) -ne $repo[2]) { throw 'Native source revision mismatch' }
}
$model = Join-Path $ProjectRoot '.cache/rnnoise-model.tar.gz'
Fetch-Checked 'https://media.xiph.org/rnnoise/models/rnnoise_data-0b50c45.tar.gz' $model '4AC81C5C0884EC4BD5907026AAAE16209B7B76CD9D7F71AF582094A2F98F4B43'
tar -xf $model -C (Join-Path $ProjectRoot '.cache/rnnoise')
if ($LASTEXITCODE) { throw 'Model extraction failed' }
Set-Content -LiteralPath (Join-Path $ProjectRoot '.cache/opus/pagvc-version.h') -Value '#define PACKAGE_VERSION "1.6.1"' -Encoding Ascii
$out = Join-Path $ProjectRoot 'native/win-x64'
New-Item -ItemType Directory -Force $out | Out-Null
function Sources($file, $variable) {
    $content = Get-Content -LiteralPath $file -Raw
    $body = [regex]::Match($content, ('(?ms)^' + $variable + '\s*=\s*(.*?)(?=^[A-Za-z_][A-Za-z_0-9]*\s*[+:]?=|^if |^endif|\z)')).Groups[1].Value
    [regex]::Matches($body, '[A-Za-z_0-9/]+\.c') | ForEach-Object Value
}
Push-Location (Join-Path $ProjectRoot '.cache/opus')
try {
    $sources = @(Sources opus_sources.mk OPUS_SOURCES) + @(Sources opus_sources.mk OPUS_SOURCES_FLOAT) + @(Sources celt_sources.mk CELT_SOURCES) + @(Sources silk_sources.mk SILK_SOURCES) + @(Sources silk_sources.mk SILK_SOURCES_FLOAT)
    & $zig cc -shared -target x86_64-windows-gnu -O3 -Wno-cpp -include pagvc-version.h -DOPUS_BUILD -DDLL_EXPORT -DUSE_ALLOCA -DHAVE_LRINTF -DHAVE_LRINT -Icelt -Isilk -Isilk/float -Iinclude @sources -o (Join-Path $out 'pagvc_opus.dll')
    if ($LASTEXITCODE) { throw 'Opus build failed' }
} finally { Pop-Location }
Push-Location (Join-Path $ProjectRoot '.cache/rnnoise')
try {
    $sources = @(Sources Makefile.am RNNOISE_SOURCES)
    & $zig cc -shared -target x86_64-windows-gnu -O3 -Wno-cpp -DRNNOISE_BUILD -DDLL_EXPORT -DWIN32 -Iinclude -Isrc @sources -o (Join-Path $out 'pagvc_rnnoise.dll')
    if ($LASTEXITCODE) { throw 'RNNoise build failed' }
} finally { Pop-Location }
Copy-Item -LiteralPath (Join-Path $ProjectRoot '.cache/opus/COPYING') -Destination (Join-Path $ProjectRoot 'licenses/Opus-LICENSE.txt')
Copy-Item -LiteralPath (Join-Path $ProjectRoot '.cache/rnnoise/COPYING') -Destination (Join-Path $ProjectRoot 'licenses/RNNoise-LICENSE.txt')
Get-FileHash -LiteralPath (Join-Path $out 'pagvc_opus.dll'), (Join-Path $out 'pagvc_rnnoise.dll') | Select-Object Hash,Path
$lockPath = Join-Path $ProjectRoot 'native/dependencies.lock.json'
$lockData = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
foreach ($item in $lockData.files) { $item.sha256 = (Get-FileHash -LiteralPath (Join-Path $out $item.file)).Hash }
$lockData | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $lockPath -Encoding UTF8
Copy-Item -LiteralPath $lockPath -Destination (Join-Path $ProjectRoot 'licenses/native-dependencies.lock.json')

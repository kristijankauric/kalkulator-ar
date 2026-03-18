param(
    [string]$SourcePath = "Build",
    [string]$TargetPath = "docs"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -Path $SourcePath -PathType Container)) {
    throw "Source path '$SourcePath' ne postoji. Prvo napravi Unity WebGL build."
}

if (-not (Test-Path -Path $TargetPath -PathType Container)) {
    New-Item -ItemType Directory -Path $TargetPath | Out-Null
}

# Drzimo docs kao cistu kopiju zadnjeg lokalnog WebGL builda.
Get-ChildItem -Path $TargetPath -Force | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $SourcePath "*") -Destination $TargetPath -Recurse -Force

Set-Content -Path (Join-Path $TargetPath ".nojekyll") -Value "" -NoNewline

# GitHub Pages ne salje uvijek Content-Encoding za .br, pa pravimo fallback nekomprimirane datoteke.
$buildDir = Join-Path $TargetPath "Build"
$brMap = @(
    @{ From = "Build.data.br"; To = "Build.data" },
    @{ From = "Build.framework.js.br"; To = "Build.framework.js" },
    @{ From = "Build.wasm.br"; To = "Build.wasm" }
)

foreach ($item in $brMap) {
    $fromPath = Join-Path $buildDir $item.From
    $toPath = Join-Path $buildDir $item.To

    if (Test-Path -Path $fromPath -PathType Leaf) {
        node -e "const fs=require('fs'); const z=require('zlib'); const src=process.argv[1]; const dst=process.argv[2]; fs.writeFileSync(dst, z.brotliDecompressSync(fs.readFileSync(src)));" "$fromPath" "$toPath"
    }
}

$indexPath = Join-Path $TargetPath "index.html"
if (Test-Path -Path $indexPath -PathType Leaf) {
    $indexContent = Get-Content -Path $indexPath -Raw
    $indexContent = $indexContent.Replace("Build/Build.data.br", "Build/Build.data")
    $indexContent = $indexContent.Replace("Build/Build.framework.js.br", "Build/Build.framework.js")
    $indexContent = $indexContent.Replace("Build/Build.wasm.br", "Build/Build.wasm")
    Set-Content -Path $indexPath -Value $indexContent -NoNewline
}

Write-Host "GitHub Pages assets spremni u '$TargetPath'."
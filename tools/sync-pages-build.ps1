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

Write-Host "GitHub Pages assets spremni u '$TargetPath'."

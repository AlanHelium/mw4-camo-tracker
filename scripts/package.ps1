$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$bundle = Join-Path $projectRoot 'artifacts/MW4-Camo-Tracker-v0.4.0-win-x64'
New-Item -ItemType Directory -Path $bundle -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'release/CamoTracker.exe') -Destination $bundle
foreach ($name in @('README.md','LICENSE','NOTICE.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $bundle }
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $bundle -Recurse -Force
'@start "" "%~dp0CamoTracker.exe" --overlay' | Set-Content -LiteralPath (Join-Path $bundle '打开游戏小窗.cmd') -Encoding ascii
$archive = Join-Path $projectRoot 'artifacts/MW4-Camo-Tracker-v0.4.0-win-x64.zip'
Compress-Archive -Path $bundle -DestinationPath $archive -CompressionLevel Optimal -Force
$digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
($digest + '  ' + (Split-Path -Leaf $archive)) | Set-Content -LiteralPath (Join-Path $projectRoot 'artifacts/SHA256SUMS.txt') -Encoding ascii
Get-Item -LiteralPath $archive | Select-Object Name,Length

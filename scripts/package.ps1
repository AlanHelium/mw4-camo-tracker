$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$version = ([xml](Get-Content -LiteralPath (Join-Path $projectRoot 'src/CamoTracker.csproj') -Raw)).Project.PropertyGroup.Version
$stem = 'MW4-Camo-Tracker-v' + $version + '-win-x64'
$bundle = Join-Path $projectRoot ('artifacts/' + $stem)
New-Item -ItemType Directory -Path $bundle -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'release/CamoTracker.exe') -Destination $bundle
foreach ($name in @('README.md','LICENSE','NOTICE.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $bundle }
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $bundle -Recurse -Force
'@start "" "%~dp0CamoTracker.exe" --overlay' | Set-Content -LiteralPath (Join-Path $bundle '打开游戏小窗.cmd') -Encoding ascii
$archive = Join-Path $projectRoot ('artifacts/' + $stem + '.zip')
Compress-Archive -Path $bundle -DestinationPath $archive -CompressionLevel Optimal -Force
$digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
($digest + '  ' + (Split-Path -Leaf $archive)) | Set-Content -LiteralPath (Join-Path $projectRoot 'artifacts/SHA256SUMS.txt') -Encoding ascii
Get-Item -LiteralPath $archive | Select-Object Name,Length

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot 'src/CamoTracker.csproj'
$releaseDir = Join-Path $projectRoot 'release'
dotnet publish $projectFile -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o $releaseDir
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
$test = Start-Process -FilePath (Join-Path $releaseDir 'CamoTracker.exe') -ArgumentList '--self-test' -PassThru -Wait -WindowStyle Hidden
if ($test.ExitCode -ne 0) { throw 'Model validation failed; see release/test-results.txt' }
Get-Content (Join-Path $releaseDir 'test-results.txt')
$dataPack = Join-Path $projectRoot 'data/catalog-pack.zip'
$channel = Get-Content -LiteralPath (Join-Path $projectRoot 'data/channel.json') -Raw | ConvertFrom-Json
if ((Get-FileHash -LiteralPath $dataPack -Algorithm SHA256).Hash.ToLowerInvariant() -ne $channel.Sha256) { throw 'Data pack hash differs from channel' }
$validation = Start-Process -FilePath (Join-Path $releaseDir 'CamoTracker.exe') -ArgumentList ('--validate-data "' + $dataPack + '"') -PassThru -Wait -WindowStyle Hidden
if ($validation.ExitCode -ne 0) { throw 'Data pack validation failed; see release/data-results.txt' }
Get-Content (Join-Path $releaseDir 'data-results.txt')

param([string]$GitHubCli = 'gh', [string]$GitExecutable = 'git')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$channel = Get-Content -LiteralPath (Join-Path $projectRoot 'data/channel.json') -Raw -Encoding utf8 | ConvertFrom-Json
$pack = Join-Path $projectRoot 'data/catalog-pack.zip'
if ((Get-FileHash -LiteralPath $pack -Algorithm SHA256).Hash.ToLowerInvariant() -ne $channel.Sha256) { throw 'Rebuild the data pack before publishing.' }
Push-Location $projectRoot
try {
    if (& $GitExecutable status --porcelain) { throw 'Commit and push the complete data source first.' }
    $head = (& $GitExecutable rev-parse HEAD).Trim()
    $remoteHead = (& $GitHubCli api repos/AlanHelium/mw4-camo-tracker/git/ref/heads/main --jq '.object.sha').Trim()
    if ($LASTEXITCODE -ne 0 -or $head -ne $remoteHead) { throw 'Public main must match this checkout.' }
    $tag = 'data-' + $channel.Revision
    $tagSha = & $GitHubCli api ('repos/AlanHelium/mw4-camo-tracker/git/ref/tags/' + $tag) --jq '.object.sha' 2>$null
    if ($LASTEXITCODE -ne 0) {
        & $GitHubCli api repos/AlanHelium/mw4-camo-tracker/git/refs --method POST -f ('ref=refs/tags/' + $tag) -f ('sha=' + $head) --silent
        if ($LASTEXITCODE -ne 0) { throw 'Could not create immutable data tag.' }
    } else {
        $tree = & $GitHubCli api ('repos/AlanHelium/mw4-camo-tracker/git/trees/' + $tagSha + '?recursive=1') | ConvertFrom-Json
        if (($tree.tree | Where-Object { $_.path -eq 'data/catalog-pack.zip' }).sha -ne $channel.Blob) { throw 'The data tag already points to a different pack. Increase the revision.' }
    }
    $notes = Join-Path ([System.IO.Path]::GetTempPath()) ('mw4-data-notes-' + [guid]::NewGuid().ToString('N') + '.md')
    try {
        [System.IO.File]::WriteAllText($notes, $channel.Notes + "`n`n在 APP 的「资料更新」中导入 catalog-pack.zip，保留已有进度。作者 Burrito，赞赏自愿。")
        & $GitHubCli release view $tag --repo AlanHelium/mw4-camo-tracker --json tagName 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { & $GitHubCli release upload $tag $pack --repo AlanHelium/mw4-camo-tracker --clobber }
        else { & $GitHubCli release create $tag $pack --repo AlanHelium/mw4-camo-tracker --verify-tag --title ('资料更新 ' + $channel.Revision) --notes-file $notes --latest=false }
        if ($LASTEXITCODE -ne 0) { throw 'Data release upload failed.' }
    } finally { if (Test-Path -LiteralPath $notes) { Remove-Item -LiteralPath $notes } }
    Write-Output 'Data published. Refresh the main channel cache at https://www.jsdelivr.com/tools/purge, then run CamoTracker.exe --verify-feed.'
} finally { Pop-Location }

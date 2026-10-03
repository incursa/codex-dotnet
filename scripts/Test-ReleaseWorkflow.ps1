[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("codex-release-tests-" + [guid]::NewGuid().ToString('N'))
$fixtureRepo = Join-Path $fixtureRoot 'repo'
$fixtureOrigin = Join-Path $fixtureRoot 'origin.git'
New-Item -ItemType Directory -Force -Path $fixtureRepo | Out-Null
$previousDotnetFunction = Get-Item Function:\dotnet -ErrorAction SilentlyContinue
$global:codexReleaseTestCalls = 0

# Only the isolated Git workflow is under test; solution validation is stubbed
# in this PowerShell process, never in the developer's environment or PATH.
function global:dotnet {
    $global:codexReleaseTestCalls++
    $global:LASTEXITCODE = 0
}

function Invoke-FixtureGit {
    param([string[]]$Arguments)
    & git -C $fixtureRepo @Arguments | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Fixture git command failed: $($Arguments -join ' ')" }
}

function Assert-Fixture {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-ReleaseRejected {
    param([hashtable]$Arguments, [string]$ExpectedMessage)
    $rejected = $false
    try { & (Join-Path $fixtureRepo 'scripts/release.ps1') @Arguments }
    catch {
        if ($_.Exception.Message -notlike "*$ExpectedMessage*") { throw }
        $rejected = $true
    }
    Assert-Fixture $rejected "Release should reject $($Arguments.Keys -join ', ')."
}

try {
    & git init --bare --initial-branch=main $fixtureOrigin | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize fixture origin.' }
    Invoke-FixtureGit @('init', '--initial-branch=main')
    Invoke-FixtureGit @('config', 'user.name', 'Release Workflow Test')
    Invoke-FixtureGit @('config', 'user.email', 'release-workflow-test@example.invalid')
    Invoke-FixtureGit @('remote', 'add', 'origin', $fixtureOrigin)
    Set-Content -LiteralPath (Join-Path $fixtureRepo '.gitattributes') -Value '* text=auto' -Encoding utf8
    New-Item -ItemType Directory -Force -Path (Join-Path $fixtureRepo 'scripts') | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'release.ps1') -Destination (Join-Path $fixtureRepo 'scripts/release.ps1')
    foreach ($package in @('Incursa.OpenAI.Codex', 'Incursa.OpenAI.Codex.Extensions')) {
        $packageDir = Join-Path $fixtureRepo "src/$package"
        New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
        Set-Content -LiteralPath (Join-Path $packageDir 'PublicAPI.Shipped.txt') -Value 'ApiV1' -Encoding utf8
        Set-Content -LiteralPath (Join-Path $packageDir 'PublicAPI.Unshipped.txt') -Value '' -Encoding utf8
    }
    $versionPath = Join-Path $fixtureRepo 'Directory.Build.props'
    Set-Content -LiteralPath $versionPath -Value '<Project><PropertyGroup><Version>2.4.0</Version></PropertyGroup></Project>' -Encoding utf8
    Invoke-FixtureGit @('add', '-A')
    Invoke-FixtureGit @('commit', '-m', 'Baseline')
    Invoke-FixtureGit @('tag', '-a', 'v2.4.0', '-m', 'Baseline')
    Invoke-FixtureGit @('push', 'origin', 'main', 'v2.4.0')
    Invoke-FixtureGit @('checkout', '-b', 'release-test')
    Add-Content -LiteralPath (Join-Path $fixtureRepo 'src/Incursa.OpenAI.Codex/PublicAPI.Shipped.txt') -Value 'ApiV2' -Encoding utf8

    $releaseScript = Join-Path $fixtureRepo 'scripts/release.ps1'
    & $releaseScript -PrepareOnly -DryRun
    Assert-Fixture ((Get-Content -LiteralPath $versionPath -Raw) -match '<Version>2.4.0</Version>') 'Dry run changed the version.'
    & $releaseScript -PrepareOnly -NoPush
    Assert-Fixture ((Get-Content -LiteralPath $versionPath -Raw) -match '<Version>2.5.0</Version>') 'Preparation did not select the API-derived minor version.'
    $preparedTag = & git -C $fixtureRepo tag --list v2.5.0
    Assert-Fixture (-not $preparedTag) 'Preparation created a release tag before merge.'
    Assert-ReleaseRejected @{ PrepareOnly = $true; NoPush = $true } 'already prepared'
    Assert-ReleaseRejected @{ Finalize = $true; NoPush = $true } 'origin/main'
    Assert-ReleaseRejected @{ PrepareOnly = $true; Finalize = $true } 'cannot be combined'

    # GitHub allows squash merges on main. Finalization must tag that merged
    # commit, not the original preparation commit with an identical tree.
    Invoke-FixtureGit @('checkout', 'main')
    Invoke-FixtureGit @('merge', '--squash', 'release-test')
    Invoke-FixtureGit @('commit', '-m', 'Squash release preparation')
    Invoke-FixtureGit @('push', 'origin', 'main')
    $mergedCommit = (& git -C $fixtureRepo rev-parse HEAD).Trim()
    Set-Content -LiteralPath (Join-Path $fixtureRepo 'untracked.txt') -Value 'dirty'
    Assert-ReleaseRejected @{ Finalize = $true; NoPush = $true } 'clean working tree'
    Invoke-FixtureGit @('add', 'untracked.txt')
    Invoke-FixtureGit @('commit', '-m', 'Track fixture file')
    Invoke-FixtureGit @('push', 'origin', 'main')
    $mergedCommit = (& git -C $fixtureRepo rev-parse HEAD).Trim()
    Invoke-FixtureGit @('checkout', '--detach', $mergedCommit)
    & $releaseScript -Finalize -DryRun
    & $releaseScript -Finalize -NoPush
    $taggedCommit = (& git -C $fixtureRepo rev-parse 'v2.5.0^{}').Trim()
    Assert-Fixture ($taggedCommit -eq $mergedCommit) 'Finalization tagged the wrong commit.'
    Assert-Fixture ((& git -C $fixtureRepo rev-parse HEAD).Trim() -eq $mergedCommit) 'Finalization created an extra commit.'
    Assert-Fixture ((Get-Content -LiteralPath $versionPath -Raw) -match '<Version>2.5.0</Version>') 'Finalization bumped the version again.'
    Assert-Fixture ($global:codexReleaseTestCalls -eq 2) 'Prepare and finalize must both run solution validation.'
    Write-Host "Release workflow checks passed. Fixture: $fixtureRoot"
}
finally {
    if ($null -ne $previousDotnetFunction) {
        Set-Item Function:\global:dotnet -Value $previousDotnetFunction.ScriptBlock
    }
    else {
        Remove-Item Function:\dotnet -ErrorAction SilentlyContinue
    }
    Remove-Variable codexReleaseTestCalls -Scope Global -ErrorAction SilentlyContinue
}

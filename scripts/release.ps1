[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$NoPush,
    [switch]$RunLiveTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionFile = Join-Path $repoRoot 'Directory.Build.props'

function Invoke-Release {
    Push-Location $repoRoot
    try {
        $latestTag = Get-LatestReleaseTag
        $currentVersion = Get-CurrentVersion -Path $versionFile
        $releaseKind = Get-PublicApiReleaseKind -BaselineTag $latestTag
        $nextVersion = Get-NextVersion -CurrentVersion $currentVersion -ReleaseKind $releaseKind

        Write-Host "Latest release tag: $latestTag"
        Write-Host "Current version: $currentVersion"
        Write-Host "Public API release kind: $releaseKind"
        Write-Host "Next version: $nextVersion"

        if ($DryRun) {
            Write-Host 'Dry run only. No files were modified.'
            return
        }

        Set-Version -Path $versionFile -Version $nextVersion

        $previousLiveTests = $env:CODEX_LIVE_TESTS
        try {
            if ($RunLiveTests) {
                $env:CODEX_LIVE_TESTS = '1'
            }

            Invoke-CheckedCommand dotnet @('test', 'Incursa.OpenAI.Codex.slnx', '-c', 'Release', '--no-restore', '-v', 'minimal')
        }
        finally {
            if ($null -eq $previousLiveTests) {
                Remove-Item Env:CODEX_LIVE_TESTS -ErrorAction SilentlyContinue
            }
            else {
                $env:CODEX_LIVE_TESTS = $previousLiveTests
            }
        }

        Invoke-CheckedCommand git @('diff', '--check')
        Invoke-CheckedCommand git @('add', '-A')
        Invoke-CheckedCommand git @('commit', '-m', "Bump version to $nextVersion")
        Invoke-CheckedCommand git @('tag', '-a', "v$nextVersion", '-m', "v$nextVersion")

        $currentBranch = (& git branch --show-current).Trim()
        if ([string]::IsNullOrWhiteSpace($currentBranch)) {
            throw 'The release script requires a checked-out branch.'
        }

        if (-not $NoPush) {
            Invoke-CheckedCommand git @('push', 'origin', $currentBranch, "v$nextVersion")
        }

        Write-Host "Release $nextVersion completed."
    }
    finally {
        Pop-Location
    }
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-LatestReleaseTag {
    $tag = (& git describe --tags --abbrev=0 --match 'v[0-9]*.[0-9]*.[0-9]*').Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($tag)) {
        throw 'Unable to determine the latest release tag.'
    }

    return $tag
}

function Get-CurrentVersion {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $content = [System.IO.File]::ReadAllText($Path)
    if ($content -match '<Version>(?<Version>[^<]+)</Version>') {
        return $Matches.Version.Trim()
    }

    throw "Unable to find a <Version> entry in $Path."
}

function Set-Version {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Version
    )

    $content = [System.IO.File]::ReadAllText($Path)
    $updated = $content -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
    if ($updated -eq $content) {
        throw "Unable to update the <Version> entry in $Path."
    }

    [System.IO.File]::WriteAllText($Path, $updated, [System.Text.UTF8Encoding]::new($false))
}

function Get-NextVersion {
    param(
        [Parameter(Mandatory)]
        [string]$CurrentVersion,

        [Parameter(Mandatory)]
        [ValidateSet('major', 'minor', 'patch')]
        [string]$ReleaseKind
    )

    $version = [version]$CurrentVersion
    if ($version.Build -lt 0) {
        throw "Version '$CurrentVersion' must have a patch component."
    }

    switch ($ReleaseKind) {
        'major' {
            return '{0}.0.0' -f ($version.Major + 1)
        }
        'minor' {
            return '{0}.{1}.0' -f $version.Major, ($version.Minor + 1)
        }
        'patch' {
            return '{0}.{1}.{2}' -f $version.Major, $version.Minor, ($version.Build + 1)
        }
    }
}

function Get-PublicApiReleaseKind {
    param(
        [Parameter(Mandatory)]
        [string]$BaselineTag
    )

    $publicApiFiles = @(
        @{
            Shipped = 'src/Incursa.OpenAI.Codex/PublicAPI.Shipped.txt'
            Unshipped = 'src/Incursa.OpenAI.Codex/PublicAPI.Unshipped.txt'
        },
        @{
            Shipped = 'src/Incursa.OpenAI.Codex.Extensions/PublicAPI.Shipped.txt'
            Unshipped = 'src/Incursa.OpenAI.Codex.Extensions/PublicAPI.Unshipped.txt'
        }
    )

    $hasMinorChange = $false
    $hasMajorChange = $false

    foreach ($filePair in $publicApiFiles) {
        Assert-EmptyFile -Path $filePair.Unshipped

        $currentLines = Get-NormalizedLines -Text ([System.IO.File]::ReadAllText((Join-Path $repoRoot $filePair.Shipped)))
        $previousLines = Get-NormalizedLines -Text (Get-GitFileContents -Tag $BaselineTag -Path $filePair.Shipped)

        $currentSet = New-LineSet -Lines $currentLines
        $previousSet = New-LineSet -Lines $previousLines

        $added = @()
        foreach ($line in $currentSet) {
            if (-not $previousSet.Contains($line)) {
                $added += $line
            }
        }

        $removed = @()
        foreach ($line in $previousSet) {
            if (-not $currentSet.Contains($line)) {
                $removed += $line
            }
        }

        Write-Host ("{0}: +{1} / -{2}" -f $filePair.Shipped, $added.Count, $removed.Count)

        if ($removed.Count -gt 0) {
            $hasMajorChange = $true
        }
        elseif ($added.Count -gt 0) {
            $hasMinorChange = $true
        }
    }

    if ($hasMajorChange) {
        return 'major'
    }

    if ($hasMinorChange) {
        return 'minor'
    }

    return 'patch'
}

function Get-GitFileContents {
    param(
        [Parameter(Mandatory)]
        [string]$Tag,

        [Parameter(Mandatory)]
        [string]$Path
    )

    $lines = & git show "${Tag}:$Path"
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read $Path from tag $Tag."
    }

    return $lines -join [Environment]::NewLine
}

function Get-NormalizedLines {
    param(
        [Parameter(Mandatory)]
        [string]$Text
    )

    return @(
        $Text -split '\r?\n' |
            ForEach-Object { $_.TrimEnd() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
}

function New-LineSet {
    param(
        [Parameter(Mandatory)]
        [string[]]$Lines
    )

    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($line in $Lines) {
        [void]$set.Add($line)
    }

    return $set
}

function Assert-EmptyFile {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $fullPath = Join-Path $repoRoot $Path
    if (-not (Test-Path $fullPath)) {
        return
    }

    $content = [System.IO.File]::ReadAllText($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($content)) {
        throw "Release blocked: $Path must be empty before cutting a release."
    }
}

Invoke-Release

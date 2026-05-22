[CmdletBinding()]
param(
    [string]$UpstreamRepoPath,
    [string]$ParityStatePath = (Join-Path $PSScriptRoot '..\quality\upstream-parity.json'),
    [string]$ReportPath = (Join-Path $PSScriptRoot '..\quality\upstream-parity-gaps.md'),
    [switch]$WriteReport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if ([string]::IsNullOrWhiteSpace($UpstreamRepoPath)) {
    $UpstreamRepoPath = $env:CODEX_UPSTREAM_REPO_PATH
}

if ([string]::IsNullOrWhiteSpace($UpstreamRepoPath)) {
    $candidatePath = [System.IO.Path]::GetFullPath((Join-Path (Split-Path $repoRoot -Parent) '..\openai\codex'))
    if (Test-Path -LiteralPath $candidatePath) {
        $UpstreamRepoPath = $candidatePath
    }
}

if ([string]::IsNullOrWhiteSpace($UpstreamRepoPath)) {
    throw 'Set CODEX_UPSTREAM_REPO_PATH or pass -UpstreamRepoPath to point at the local openai/codex checkout.'
}

$resolvedUpstreamRepoPath = (Resolve-Path -LiteralPath $UpstreamRepoPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $resolvedUpstreamRepoPath '.git'))) {
    throw "The upstream path '$resolvedUpstreamRepoPath' is not a git checkout."
}

if (-not (Test-Path -LiteralPath $ParityStatePath)) {
    throw "Parity state file not found: $ParityStatePath"
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & git -C $resolvedUpstreamRepoPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $message = $output -join [Environment]::NewLine
        throw "git -C `"$resolvedUpstreamRepoPath`" $($Arguments -join ' ') failed with exit code $LASTEXITCODE.`n$message"
    }

    return @($output | Where-Object { $_ -ne $null -and $_ -ne '' })
}

function Get-LimitedLines {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Lines,
        [int]$Limit = 12
    )

    if ($Lines.Count -le $Limit) {
        return $Lines
    }

    $visible = @($Lines | Select-Object -First $Limit)
    $visible += "... ($($Lines.Count - $Limit) more)"
    return $visible
}

function Append-Line {
    param(
        [Parameter(Mandatory = $true)]
        [System.Text.StringBuilder]$Builder,
        [string]$Text = ''
    )

    [void]$Builder.AppendLine($Text)
}

$state = Get-Content -LiteralPath $ParityStatePath -Raw | ConvertFrom-Json
$upstreamHead = (Invoke-Git -Arguments @('rev-parse', 'HEAD') | Select-Object -First 1).Trim()

$upstreamEntries = @(
    [pscustomobject]@{
        Name = 'python'
        DisplayName = 'Python SDK'
        Entry = $state.upstreams.python
    },
    [pscustomobject]@{
        Name = 'typescript'
        DisplayName = 'TypeScript SDK'
        Entry = $state.upstreams.typescript
    }
)

$sections = New-Object System.Collections.Generic.List[object]
$hasUpdates = $false

foreach ($upstream in $upstreamEntries) {
    $trackedPath = [string]$upstream.Entry.trackedPath
    $baselineCommit = [string]$upstream.Entry.lastReviewedCommit

    $commitLines = @(
        Invoke-Git -Arguments @('log', '--oneline', "$baselineCommit..HEAD", '--', $trackedPath)
    )
    $fileLines = @(
        Invoke-Git -Arguments @('diff', '--name-only', "$baselineCommit..HEAD", '--', $trackedPath)
    )

    if ($commitLines.Count -gt 0 -or $fileLines.Count -gt 0) {
        $hasUpdates = $true
    }

    $sections.Add([pscustomobject]@{
        DisplayName = $upstream.DisplayName
        TrackedPath = $trackedPath
        BaselineCommit = $baselineCommit
        CommitLines = $commitLines
        FileLines = $fileLines
    })
}

$statusText = if ($hasUpdates) {
    'updates found after the latest Python and TypeScript upstream comparison.'
}
else {
    'current after the latest Python and TypeScript upstream comparison.'
}

$report = New-Object System.Text.StringBuilder
Append-Line -Builder $report -Text '# Upstream Parity Review'
Append-Line -Builder $report -Text ''
Append-Line -Builder $report -Text "Status: $statusText"
Append-Line -Builder $report -Text ''
Append-Line -Builder $report -Text '## Current Read'
Append-Line -Builder $report -Text ''
Append-Line -Builder $report -Text "- Upstream repo: ``$resolvedUpstreamRepoPath``"
Append-Line -Builder $report -Text "- Reviewed baseline commit: ``$($state.upstreams.python.lastReviewedCommit)``"
Append-Line -Builder $report -Text "- Upstream head commit: ``$upstreamHead``"
Append-Line -Builder $report -Text '- The Python SDK is the primary source of truth for this comparison.'
Append-Line -Builder $report -Text '- The TypeScript SDK is still checked because both SDKs live in the same upstream monorepo.'
Append-Line -Builder $report -Text ''
Append-Line -Builder $report -Text '## Compared Paths'
Append-Line -Builder $report -Text ''

foreach ($section in $sections) {
    Append-Line -Builder $report -Text "### $($section.DisplayName)"
    Append-Line -Builder $report -Text ''
    Append-Line -Builder $report -Text "- Tracked path: ``$($section.TrackedPath)``"
    Append-Line -Builder $report -Text "- Baseline commit: ``$($section.BaselineCommit)``"
    Append-Line -Builder $report -Text "- Latest commit range: ``$($section.BaselineCommit)..$upstreamHead``"
    Append-Line -Builder $report -Text "- Commit count: $($section.CommitLines.Count)"
    Append-Line -Builder $report -Text "- Changed file count: $($section.FileLines.Count)"
    Append-Line -Builder $report -Text ''

    if ($section.CommitLines.Count -gt 0) {
        Append-Line -Builder $report -Text '#### Commit Lines'
        Append-Line -Builder $report -Text ''
        foreach ($line in (Get-LimitedLines -Lines $section.CommitLines)) {
            Append-Line -Builder $report -Text "- $line"
        }
        Append-Line -Builder $report -Text ''
    }

    if ($section.FileLines.Count -gt 0) {
        Append-Line -Builder $report -Text '#### Changed Files'
        Append-Line -Builder $report -Text ''
        foreach ($line in (Get-LimitedLines -Lines $section.FileLines)) {
            Append-Line -Builder $report -Text "- $line"
        }
        Append-Line -Builder $report -Text ''
    }
}

if ($hasUpdates) {
    Append-Line -Builder $report -Text '## Next Steps'
    Append-Line -Builder $report -Text ''
    Append-Line -Builder $report -Text '- Review the Python SDK diff first.'
    Append-Line -Builder $report -Text '- Apply the matching .NET changes in `src/Incursa.OpenAI.Codex` and `tests/Incursa.OpenAI.Codex.Tests`.'
    Append-Line -Builder $report -Text '- Refresh `quality/upstream-parity.json` after the .NET implementation is updated.'
    Append-Line -Builder $report -Text '- Re-run this review once the local branch has caught up.'
    Append-Line -Builder $report -Text ''
}
else {
    Append-Line -Builder $report -Text '## Notes'
    Append-Line -Builder $report -Text ''
    Append-Line -Builder $report -Text '- No upstream SDK changes are pending relative to the recorded baseline.'
    Append-Line -Builder $report -Text '- Keep the Python SDK as the primary review source when the next upstream delta lands.'
    Append-Line -Builder $report -Text ''
}

if ($WriteReport) {
    $reportDirectory = Split-Path -Parent $ReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
        New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($ReportPath, $report.ToString(), [System.Text.UTF8Encoding]::new($false))
}

if ($env:GITHUB_OUTPUT) {
    "status=$($hasUpdates ? 'updates-found' : 'current')" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "upstream_head=$upstreamHead" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "report_path=$ReportPath" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
}

Write-Host "Upstream parity review status: $($hasUpdates ? 'updates-found' : 'current')"

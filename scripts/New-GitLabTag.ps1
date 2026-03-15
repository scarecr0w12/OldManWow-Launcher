param(
    [string]$CommitSha = $env:CI_COMMIT_SHA,
    [string]$RemoteName = 'origin',
    [string]$TagPrefix = 'v',
    [string]$InitialVersion = '1.0.0',
    [string]$ProjectId = $env:CI_PROJECT_ID,
    [string]$GitLabApiUrl = $env:CI_API_V4_URL,
    [string]$GitLabToken = $env:GITLAB_TOKEN,
    [string]$JobToken = $env:CI_JOB_TOKEN
)

$ErrorActionPreference = 'Stop'

function Get-VersionObject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VersionText
    )

    $normalized = $VersionText.Trim()
    if ($normalized.StartsWith($TagPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring($TagPrefix.Length)
    }

    $parts = $normalized.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Length -lt 3 -or $parts.Length -gt 4) {
        return $null
    }

    foreach ($part in $parts) {
        if ($part -notmatch '^\d+$') {
            return $null
        }
    }

    while ($parts.Length -lt 4) {
        $parts += '0'
    }

    return [Version]::Parse([string]::Join('.', $parts))
}

function Get-ReleaseTagName {
    param(
        [Parameter(Mandatory = $true)]
        [Version]$Version
    )

    return '{0}{1}.{2}.{3}' -f $TagPrefix, $Version.Major, $Version.Minor, $Version.Build
}

function Get-ApiHeaders {
    if (![string]::IsNullOrWhiteSpace($GitLabToken)) {
        return @{ 'PRIVATE-TOKEN' = $GitLabToken }
    }

    if (![string]::IsNullOrWhiteSpace($JobToken)) {
        return @{ 'JOB-TOKEN' = $JobToken }
    }

    throw 'Set GITLAB_TOKEN or allow CI_JOB_TOKEN to create repository tags.'
}

function Get-ResponseStatusCode {
    param(
        [Parameter(Mandatory = $true)]
        $ExceptionRecord
    )

    if ($null -eq $ExceptionRecord.Exception.Response) {
        return $null
    }

    return [int]$ExceptionRecord.Exception.Response.StatusCode
}

if ([string]::IsNullOrWhiteSpace($CommitSha)) {
    throw 'GitLab CI environment variables are missing. Run this script from a branch pipeline.'
}

if ([string]::IsNullOrWhiteSpace($ProjectId)) {
    throw 'CI_PROJECT_ID is required.'
}

if ([string]::IsNullOrWhiteSpace($GitLabApiUrl)) {
    throw 'CI_API_V4_URL is required.'
}

$releaseTagPattern = '^{0}\d+\.\d+\.\d+(\.\d+)?$' -f [System.Text.RegularExpressions.Regex]::Escape($TagPrefix)
$remoteCheck = git remote get-url $RemoteName 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($remoteCheck)) {
    throw "Git remote '$RemoteName' was not found."
}

git fetch --force --tags $RemoteName | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Fetching tags from '$RemoteName' failed."
}

$existingCommitTags = @(git tag --points-at $CommitSha)
if ($LASTEXITCODE -ne 0) {
    throw "Reading tags for commit $CommitSha failed."
}

if ($existingCommitTags | Where-Object { $_ -match $releaseTagPattern }) {
    Write-Host "Commit $CommitSha already has a release tag. Skipping tag creation."
    exit 0
}

$latestVersion = Get-VersionObject -VersionText $InitialVersion
if ($null -eq $latestVersion) {
    throw "InitialVersion '$InitialVersion' is invalid. Use semantic versions like 1.0.0."
}

foreach ($tag in @(git tag --list)) {
    $parsedVersion = Get-VersionObject -VersionText $tag
    if ($null -ne $parsedVersion -and $parsedVersion -gt $latestVersion) {
        $latestVersion = $parsedVersion
    }
}

$nextVersion = [Version]::new($latestVersion.Major, $latestVersion.Minor, $latestVersion.Build + 1, 0)
$nextTagName = Get-ReleaseTagName -Version $nextVersion
$encodedProjectId = [System.Uri]::EscapeDataString($ProjectId)
$tagUri = '{0}/projects/{1}/repository/tags?tag_name={2}&ref={3}' -f $GitLabApiUrl.TrimEnd('/'), $encodedProjectId, [System.Uri]::EscapeDataString($nextTagName), [System.Uri]::EscapeDataString($CommitSha)

try {
    Invoke-RestMethod -Method Post -Headers (Get-ApiHeaders) -Uri $tagUri | Out-Null
    Write-Host "Created release tag $nextTagName for commit $CommitSha"
}
catch {
    $statusCode = Get-ResponseStatusCode -ExceptionRecord $_
    if ($statusCode -eq 400) {
        Write-Host "Release tag $nextTagName already exists. Skipping duplicate tag creation."
        exit 0
    }

    throw
}

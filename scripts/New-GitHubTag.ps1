param(
    [string]$CommitSha = $env:GITHUB_SHA,
    [string]$RemoteName = 'origin',
    [string]$TagPrefix = 'v',
    [string]$InitialVersion = '1.0.0'
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

function Set-ReleaseTagOutput {
    param(
        [string]$TagName
    )

    if (![string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        Add-Content -Path $env:GITHUB_OUTPUT -Value "release_tag=$TagName"
    }
}

if ([string]::IsNullOrWhiteSpace($CommitSha)) {
    throw 'GitHub Actions environment variables are missing. Run this script from a branch workflow.'
}

if ([string]::IsNullOrWhiteSpace($RemoteName)) {
    throw 'RemoteName is required.'
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
    Set-ReleaseTagOutput -TagName ''
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

git tag $nextTagName $CommitSha
if ($LASTEXITCODE -ne 0) {
    throw "Creating tag $nextTagName for commit $CommitSha failed."
}

git push $RemoteName "refs/tags/$nextTagName"
if ($LASTEXITCODE -ne 0) {
    throw "Pushing tag $nextTagName to '$RemoteName' failed."
}

Set-ReleaseTagOutput -TagName $nextTagName
Write-Host "Created release tag $nextTagName for commit $CommitSha"

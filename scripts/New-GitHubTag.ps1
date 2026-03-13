param(
    [string]$ApiUrl = 'https://api.github.com',
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [string]$CommitSha = $env:GITHUB_SHA,
    [string]$Token = $env:GITHUB_TOKEN,
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

if ([string]::IsNullOrWhiteSpace($Repository) -or [string]::IsNullOrWhiteSpace($CommitSha)) {
    throw 'GitHub Actions environment variables are missing. Run this script from a branch workflow.'
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'Missing GITHUB_TOKEN. Grant the workflow contents: write permissions before enabling automatic tagging.'
}

$headers = @{
    Accept = 'application/vnd.github+json'
    Authorization = "Bearer $Token"
    'User-Agent' = 'Wow-Launcher-GitHub-Actions'
    'X-GitHub-Api-Version' = '2022-11-28'
}

$releaseTagPattern = '^{0}\d+\.\d+\.\d+(\.\d+)?$' -f [System.Text.RegularExpressions.Regex]::Escape($TagPrefix)
$tags = @()
$page = 1

while ($true) {
    $tagsEndpoint = "$ApiUrl/repos/$Repository/tags?per_page=100&page=$page"
    $pageTags = @(Invoke-RestMethod -Method Get -Uri $tagsEndpoint -Headers $headers)
    if ($pageTags.Count -eq 0) {
        break
    }

    $tags += $pageTags

    if ($pageTags.Count -lt 100) {
        break
    }

    $page += 1
}

if ($tags | Where-Object { $_.name -match $releaseTagPattern -and $_.commit -and $_.commit.sha -eq $CommitSha }) {
    Set-ReleaseTagOutput -TagName ''
    Write-Host "Commit $CommitSha already has a release tag. Skipping tag creation."
    exit 0
}

$latestVersion = Get-VersionObject -VersionText $InitialVersion
if ($null -eq $latestVersion) {
    throw "InitialVersion '$InitialVersion' is invalid. Use semantic versions like 1.0.0."
}

foreach ($tag in $tags) {
    $parsedVersion = Get-VersionObject -VersionText $tag.name
    if ($null -ne $parsedVersion -and $parsedVersion -gt $latestVersion) {
        $latestVersion = $parsedVersion
    }
}

$nextVersion = [Version]::new($latestVersion.Major, $latestVersion.Minor, $latestVersion.Build + 1, 0)
$nextTagName = Get-ReleaseTagName -Version $nextVersion
$createRefEndpoint = "$ApiUrl/repos/$Repository/git/refs"
$body = @{
    ref = "refs/tags/$nextTagName"
    sha = $CommitSha
} | ConvertTo-Json

try {
    Invoke-RestMethod -Method Post -Uri $createRefEndpoint -Headers $headers -Body $body -ContentType 'application/json' | Out-Null
}
catch {
    $message = $_.Exception.Message
    if ($_.ErrorDetails -and ![string]::IsNullOrWhiteSpace($_.ErrorDetails.Message)) {
        $message = $message + [Environment]::NewLine + $_.ErrorDetails.Message
    }

    throw $message
}

Set-ReleaseTagOutput -TagName $nextTagName
Write-Host "Created release tag $nextTagName for commit $CommitSha"

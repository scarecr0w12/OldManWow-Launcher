param(
    [string]$ApiUrl = $env:CI_API_V4_URL,
    [string]$ProjectId = $env:CI_PROJECT_ID,
    [string]$CommitSha = $env:CI_COMMIT_SHA,
    [string]$Token = $env:GITLAB_API_TOKEN,
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

if ([string]::IsNullOrWhiteSpace($ApiUrl) -or [string]::IsNullOrWhiteSpace($ProjectId) -or [string]::IsNullOrWhiteSpace($CommitSha)) {
    throw 'GitLab CI environment variables are missing. Run this script from a branch pipeline.'
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'Missing GITLAB_API_TOKEN. Add a protected CI/CD variable with API scope before enabling automatic tagging.'
}

$headers = @{ 'PRIVATE-TOKEN' = $Token }
$encodedProjectId = [Uri]::EscapeDataString($ProjectId)
$encodedCommitSha = [Uri]::EscapeDataString($CommitSha)
$commitRefsEndpoint = "$ApiUrl/projects/$encodedProjectId/repository/commits/$encodedCommitSha/refs?type=tag"
$tagsEndpoint = "$ApiUrl/projects/$encodedProjectId/repository/tags?per_page=100"
$createTagEndpoint = "$ApiUrl/projects/$encodedProjectId/repository/tags"

$existingCommitTags = @(Invoke-RestMethod -Method Get -Uri $commitRefsEndpoint -Headers $headers)
if ($existingCommitTags | Where-Object { $_.name -match ('^{0}\d+\.\d+\.\d+(\.\d+)?$' -f [System.Text.RegularExpressions.Regex]::Escape($TagPrefix)) }) {
    Write-Host "Commit $CommitSha already has a release tag. Skipping tag creation."
    exit 0
}

$latestVersion = Get-VersionObject -VersionText $InitialVersion
if ($null -eq $latestVersion) {
    throw "InitialVersion '$InitialVersion' is invalid. Use semantic versions like 1.0.0."
}

$tags = @(Invoke-RestMethod -Method Get -Uri $tagsEndpoint -Headers $headers)
foreach ($tag in $tags) {
    $parsedVersion = Get-VersionObject -VersionText $tag.name
    if ($null -ne $parsedVersion -and $parsedVersion -gt $latestVersion) {
        $latestVersion = $parsedVersion
    }
}

$nextVersion = [Version]::new($latestVersion.Major, $latestVersion.Minor, $latestVersion.Build + 1, 0)
$nextTagName = Get-ReleaseTagName -Version $nextVersion

$body = @{
    tag_name = $nextTagName
    ref = $CommitSha
    message = "Automated launcher release $nextTagName"
}

Invoke-RestMethod -Method Post -Uri $createTagEndpoint -Headers $headers -Body $body | Out-Null
Write-Host "Created release tag $nextTagName for commit $CommitSha"

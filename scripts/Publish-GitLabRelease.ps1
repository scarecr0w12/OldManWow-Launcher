param(
    [string]$ReleaseTag = $env:CI_COMMIT_TAG,
    [string]$ProjectId = $env:CI_PROJECT_ID,
    [string]$GitLabApiUrl = $env:CI_API_V4_URL,
    [string]$ProjectUrl = $env:CI_PROJECT_URL,
    [string]$ArtifactPath = 'artifacts/Wow-Launcher.exe',
    [string]$JobId = $env:CI_JOB_ID,
    [string]$GitLabToken = $env:GITLAB_TOKEN,
    [string]$JobToken = $env:CI_JOB_TOKEN
)

$ErrorActionPreference = 'Stop'

function Get-ApiHeaders {
    if (![string]::IsNullOrWhiteSpace($GitLabToken)) {
        return @{ 'PRIVATE-TOKEN' = $GitLabToken }
    }

    if (![string]::IsNullOrWhiteSpace($JobToken)) {
        return @{ 'JOB-TOKEN' = $JobToken }
    }

    throw 'Set GITLAB_TOKEN or allow CI_JOB_TOKEN to create releases.'
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

if ([string]::IsNullOrWhiteSpace($ReleaseTag)) {
    throw 'CI_COMMIT_TAG is required.'
}

if ([string]::IsNullOrWhiteSpace($ProjectId)) {
    throw 'CI_PROJECT_ID is required.'
}

if ([string]::IsNullOrWhiteSpace($GitLabApiUrl)) {
    throw 'CI_API_V4_URL is required.'
}

if ([string]::IsNullOrWhiteSpace($ProjectUrl)) {
    throw 'CI_PROJECT_URL is required.'
}

if ([string]::IsNullOrWhiteSpace($JobId)) {
    throw 'CI_JOB_ID is required.'
}

$normalizedArtifactPath = $ArtifactPath.Replace('\', '/')
$artifactUrl = '{0}/-/jobs/{1}/artifacts/raw/{2}' -f $ProjectUrl.TrimEnd('/'), $JobId, $normalizedArtifactPath
$headers = Get-ApiHeaders
$headers['Content-Type'] = 'application/json'
$encodedProjectId = [System.Uri]::EscapeDataString($ProjectId)
$encodedReleaseTag = [System.Uri]::EscapeDataString($ReleaseTag)
$releaseUri = '{0}/projects/{1}/releases/{2}' -f $GitLabApiUrl.TrimEnd('/'), $encodedProjectId, $encodedReleaseTag
$releasesBaseUri = '{0}/projects/{1}/releases' -f $GitLabApiUrl.TrimEnd('/'), $encodedProjectId
$releaseName = "Launcher $ReleaseTag"
$releaseDescription = "Automated launcher release for $ReleaseTag."

$createPayload = @{
    name = $releaseName
    tag_name = $ReleaseTag
    description = $releaseDescription
    assets = @{
        links = @(
            @{
                name = 'Wow-Launcher.exe'
                url = $artifactUrl
                link_type = 'package'
            }
        )
    }
} | ConvertTo-Json -Depth 6

$updatePayload = @{
    name = $releaseName
    description = $releaseDescription
    assets = @{
        links = @(
            @{
                name = 'Wow-Launcher.exe'
                url = $artifactUrl
                link_type = 'package'
            }
        )
    }
} | ConvertTo-Json -Depth 6

$releaseExists = $false

try {
    Invoke-RestMethod -Method Get -Headers $headers -Uri $releaseUri | Out-Null
    $releaseExists = $true
}
catch {
    $statusCode = Get-ResponseStatusCode -ExceptionRecord $_
    if ($statusCode -ne 404) {
        throw
    }
}

if ($releaseExists) {
    Invoke-RestMethod -Method Put -Headers $headers -Uri $releaseUri -Body $updatePayload | Out-Null
    Write-Host "Updated GitLab release for $ReleaseTag"
}
else {
    Invoke-RestMethod -Method Post -Headers $headers -Uri $releasesBaseUri -Body $createPayload | Out-Null
    Write-Host "Created GitLab release for $ReleaseTag"
}

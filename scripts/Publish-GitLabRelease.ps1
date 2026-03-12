param(
    [string]$ApiUrl = $env:CI_API_V4_URL,
    [string]$ProjectId = $env:CI_PROJECT_ID,
    [string]$ProjectUrl = $env:CI_PROJECT_URL,
    [string]$TagName = $env:CI_COMMIT_TAG,
    [string]$AssetName = 'Wow-Launcher.exe',
    [string]$AssetPath = 'release\Wow-Launcher.exe',
    [string]$Token = $env:GITLAB_API_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ApiUrl) -or [string]::IsNullOrWhiteSpace($ProjectId) -or [string]::IsNullOrWhiteSpace($ProjectUrl) -or [string]::IsNullOrWhiteSpace($TagName)) {
    throw 'GitLab CI environment variables are missing. Run this script from a GitLab tag pipeline.'
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'Missing GITLAB_API_TOKEN. Add a protected CI/CD variable with API scope before running release pipelines.'
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$assetFullPath = Join-Path $repoRoot $AssetPath
if (!(Test-Path $assetFullPath)) {
    throw "Release asset was not found: $assetFullPath"
}

$assetUrl = "$ProjectUrl/-/jobs/$($env:CI_JOB_ID)/artifacts/raw/$($AssetPath -replace '\\', '/')"
$releaseName = "Launcher $TagName"
$description = "Automated launcher release for $TagName"
$encodedTag = [Uri]::EscapeDataString($TagName)
$releasesEndpoint = "$ApiUrl/projects/$ProjectId/releases"
$headers = @{ 'PRIVATE-TOKEN' = $Token }

$existingRelease = $null
try {
    $existingRelease = Invoke-RestMethod -Method Get -Uri "$releasesEndpoint/$encodedTag" -Headers $headers
}
catch {
    $statusCode = $null
    if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
        $statusCode = [int]$_.Exception.Response.StatusCode
    }

    if ($statusCode -ne 404) {
        throw
    }
}

if ($existingRelease) {
    Write-Host "Release $TagName already exists. Skipping creation."
    exit 0
}

$payload = @{
    name = $releaseName
    tag_name = $TagName
    description = $description
    assets = @{
        links = @(
            @{
                name = $AssetName
                url = $assetUrl
                link_type = 'other'
            }
        )
    }
}

$body = $payload | ConvertTo-Json -Depth 8
Invoke-RestMethod -Method Post -Uri $releasesEndpoint -Headers $headers -Body $body -ContentType 'application/json' | Out-Null

Write-Host "Created GitLab release $TagName with asset link $assetUrl"
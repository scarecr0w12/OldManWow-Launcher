param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyInfoPath,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseVersion
)

$ErrorActionPreference = 'Stop'

function ConvertTo-AssemblyVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $normalized = $Version.Trim()
    if ($normalized.StartsWith('v', [System.StringComparison]::OrdinalIgnoreCase)) {
        $normalized = $normalized.Substring(1)
    }

    $parts = $normalized.Split('.', [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Length -lt 2 -or $parts.Length -gt 4) {
        throw "ReleaseVersion '$Version' must contain between 2 and 4 numeric segments."
    }

    foreach ($part in $parts) {
        if ($part -notmatch '^\d+$') {
            throw "ReleaseVersion '$Version' contains a non-numeric segment."
        }
    }

    while ($parts.Length -lt 4) {
        $parts += '0'
    }

    return [string]::Join('.', $parts)
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$assemblyInfoFullPath = Join-Path $repoRoot $AssemblyInfoPath
if (!(Test-Path $assemblyInfoFullPath)) {
    throw "AssemblyInfo.cs was not found: $assemblyInfoFullPath"
}

$assemblyVersion = ConvertTo-AssemblyVersion -Version $ReleaseVersion
$informationalVersion = $ReleaseVersion.TrimStart('v', 'V')
$content = [System.IO.File]::ReadAllText($assemblyInfoFullPath)

$updatedContent = $content
$updatedContent = [System.Text.RegularExpressions.Regex]::Replace($updatedContent, '\[assembly:\s*AssemblyVersion\("[^"]+"\)\]', "[assembly: AssemblyVersion(`"$assemblyVersion`")]")
$updatedContent = [System.Text.RegularExpressions.Regex]::Replace($updatedContent, '\[assembly:\s*AssemblyFileVersion\("[^"]+"\)\]', "[assembly: AssemblyFileVersion(`"$assemblyVersion`")]")

if ($updatedContent -match '\[assembly:\s*AssemblyInformationalVersion\("[^"]+"\)\]') {
    $updatedContent = [System.Text.RegularExpressions.Regex]::Replace($updatedContent, '\[assembly:\s*AssemblyInformationalVersion\("[^"]+"\)\]', "[assembly: AssemblyInformationalVersion(`"$informationalVersion`")]")
}
else {
    $updatedContent = $updatedContent.TrimEnd() + [Environment]::NewLine + "[assembly: AssemblyInformationalVersion(`"$informationalVersion`")]" + [Environment]::NewLine
}

if ($updatedContent -ne $content) {
    [System.IO.File]::WriteAllText($assemblyInfoFullPath, $updatedContent)
    Write-Host "Updated assembly version to $assemblyVersion"
}
else {
    Write-Host "Assembly version already matches $assemblyVersion"
}

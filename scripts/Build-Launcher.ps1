param(
    [string]$ProjectPath = "Wow-Launcher\Wow-Launcher.csproj",
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts",
    [string]$ReleaseVersion = ""
)

$ErrorActionPreference = 'Stop'

function Get-MSBuildPath {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($msbuild) {
            return $msbuild
        }
    }

    $frameworkMsbuild = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe'
    if (Test-Path $frameworkMsbuild) {
        return $frameworkMsbuild
    }

    throw 'MSBuild.exe was not found. Install Visual Studio Build Tools or configure a Windows runner with MSBuild available.'
}

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

function Set-AssemblyVersionContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AssemblyInfoPath,

        [Parameter(Mandatory = $true)]
        [string]$ReleaseVersion
    )

    $originalContent = [System.IO.File]::ReadAllText($AssemblyInfoPath)
    $assemblyVersion = ConvertTo-AssemblyVersion -Version $ReleaseVersion
    $informationalVersion = $ReleaseVersion.TrimStart('v', 'V')

    $updatedContent = $originalContent
    $updatedContent = [System.Text.RegularExpressions.Regex]::Replace($updatedContent, '\[assembly:\s*AssemblyVersion\("[^"]+"\)\]', "[assembly: AssemblyVersion(`"$assemblyVersion`")]")
    $updatedContent = [System.Text.RegularExpressions.Regex]::Replace($updatedContent, '\[assembly:\s*AssemblyFileVersion\("[^"]+"\)\]', "[assembly: AssemblyFileVersion(`"$assemblyVersion`")]")

    if ($updatedContent -match '\[assembly:\s*AssemblyInformationalVersion\("[^"]+"\)\]') {
        $updatedContent = [System.Text.RegularExpressions.Regex]::Replace($updatedContent, '\[assembly:\s*AssemblyInformationalVersion\("[^"]+"\)\]', "[assembly: AssemblyInformationalVersion(`"$informationalVersion`")]")
    }
    else {
        $updatedContent = $updatedContent.TrimEnd() + [Environment]::NewLine + "[assembly: AssemblyInformationalVersion(`"$informationalVersion`")]" + [Environment]::NewLine
    }

    [System.IO.File]::WriteAllText($AssemblyInfoPath, $updatedContent)
    return $originalContent
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectFullPath = Join-Path $repoRoot $ProjectPath
if (!(Test-Path $projectFullPath)) {
    throw "Project file was not found: $projectFullPath"
}

$assemblyInfoPath = Join-Path (Split-Path -Parent $projectFullPath) 'Properties\AssemblyInfo.cs'
$originalAssemblyInfo = $null

try {
    if (![string]::IsNullOrWhiteSpace($ReleaseVersion)) {
        if (!(Test-Path $assemblyInfoPath)) {
            throw "AssemblyInfo.cs was not found: $assemblyInfoPath"
        }

        $originalAssemblyInfo = Set-AssemblyVersionContent -AssemblyInfoPath $assemblyInfoPath -ReleaseVersion $ReleaseVersion
        Write-Host "Stamped assembly version from tag $ReleaseVersion"
    }

    $msbuildPath = Get-MSBuildPath
    Write-Host "Using MSBuild: $msbuildPath"

    & $msbuildPath $projectFullPath '/t:Build' '/p:Platform=AnyCPU' "/p:Configuration=$Configuration"
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE"
    }

    $projectDirectory = Split-Path -Parent $projectFullPath
    $launcherPath = Join-Path $projectDirectory "bin\$Configuration\Wow-Launcher.exe"
    if (!(Test-Path $launcherPath)) {
        throw "Built launcher executable was not found: $launcherPath"
    }

    $outputFullPath = Join-Path $repoRoot $OutputDirectory
    New-Item -ItemType Directory -Force -Path $outputFullPath | Out-Null
    Copy-Item $launcherPath (Join-Path $outputFullPath 'Wow-Launcher.exe') -Force

    Write-Host "Copied launcher artifact to $(Join-Path $outputFullPath 'Wow-Launcher.exe')"
}
finally {
    if ($null -ne $originalAssemblyInfo -and (Test-Path $assemblyInfoPath)) {
        [System.IO.File]::WriteAllText($assemblyInfoPath, $originalAssemblyInfo)
    }
}
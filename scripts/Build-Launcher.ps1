param(
    [string]$ProjectPath = "Wow-Launcher\Wow-Launcher.csproj",
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts"
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

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectFullPath = Join-Path $repoRoot $ProjectPath
if (!(Test-Path $projectFullPath)) {
    throw "Project file was not found: $projectFullPath"
}

$msbuildPath = Get-MSBuildPath
Write-Host "Using MSBuild: $msbuildPath"

& $msbuildPath $projectFullPath '/t:Build' "/p:Configuration=$Configuration" '/p:Platform=AnyCPU'
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
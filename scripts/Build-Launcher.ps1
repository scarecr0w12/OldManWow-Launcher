param(
    [string]$ProjectPath = "Wow-Launcher\Wow-Launcher.csproj",
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts",
    [string]$ReleaseVersion = "",
    [string]$CodeSigningCertificatePath = $env:CODE_SIGNING_CERTIFICATE_PATH,
    [string]$CodeSigningCertificateBase64 = $env:CODE_SIGNING_CERTIFICATE_BASE64,
    [string]$CodeSigningCertificatePassword = $env:CODE_SIGNING_CERTIFICATE_PASSWORD,
    [string]$CodeSigningTimestampUrl = $env:CODE_SIGNING_TIMESTAMP_URL
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

function Get-SignToolPath {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $kitRoots = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'),
        (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\8.1\bin')
    )

    foreach ($kitRoot in $kitRoots) {
        if (!(Test-Path $kitRoot)) {
            continue
        }

        $candidate = Get-ChildItem -Path $kitRoot -Recurse -Filter 'signtool.exe' -File -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($candidate) {
            return $candidate.FullName
        }
    }

    throw 'signtool.exe was not found. Install the Windows SDK or run the build on a machine with SignTool available.'
}

function Resolve-CodeSigningCertificatePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot,

        [string]$CertificatePath,
        [string]$CertificateBase64
    )

    if (![string]::IsNullOrWhiteSpace($CertificatePath)) {
        $resolvedPath = $CertificatePath
        if (![System.IO.Path]::IsPathRooted($resolvedPath)) {
            $resolvedPath = Join-Path $RepositoryRoot $resolvedPath
        }

        if (!(Test-Path $resolvedPath)) {
            throw "Code signing certificate was not found: $resolvedPath"
        }

        return (Resolve-Path $resolvedPath).Path
    }

    if (![string]::IsNullOrWhiteSpace($CertificateBase64)) {
        $temporaryCertificatePath = Join-Path ([System.IO.Path]::GetTempPath()) ("wow-launcher-signing-{0}.pfx" -f [System.Guid]::NewGuid().ToString('N'))

        try {
            [System.IO.File]::WriteAllBytes($temporaryCertificatePath, [System.Convert]::FromBase64String($CertificateBase64))
        }
        catch {
            throw 'CODE_SIGNING_CERTIFICATE_BASE64 is not valid base64 PFX content.'
        }

        return $temporaryCertificatePath
    }

    return $null
}

function Sign-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [string]$CertificatePath,
        [string]$CertificatePassword,
        [string]$TimestampUrl
    )

    if ([string]::IsNullOrWhiteSpace($CertificatePath)) {
        Write-Host 'Code signing skipped because no signing certificate was provided.'
        return
    }

    $signToolPath = Get-SignToolPath
    $arguments = @('sign', '/fd', 'SHA256', '/f', $CertificatePath)

    if (![string]::IsNullOrWhiteSpace($CertificatePassword)) {
        $arguments += @('/p', $CertificatePassword)
    }

    if (![string]::IsNullOrWhiteSpace($TimestampUrl)) {
        $arguments += @('/tr', $TimestampUrl, '/td', 'SHA256')
    }

    $arguments += $FilePath

    & $signToolPath @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Code signing failed with exit code $LASTEXITCODE"
    }

    $signature = Get-AuthenticodeSignature -FilePath $FilePath
    if ($signature.Status -eq [System.Management.Automation.SignatureStatus]::NotSigned) {
        throw "The launcher artifact was not signed: $FilePath"
    }

    Write-Host "Signed launcher artifact: $FilePath"
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
$temporarySigningCertificatePath = $null

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
    $artifactPath = Join-Path $outputFullPath 'Wow-Launcher.exe'
    Copy-Item $launcherPath $artifactPath -Force

    $temporarySigningCertificatePath = Resolve-CodeSigningCertificatePath -RepositoryRoot $repoRoot -CertificatePath $CodeSigningCertificatePath -CertificateBase64 $CodeSigningCertificateBase64

    if ([string]::IsNullOrWhiteSpace($CodeSigningTimestampUrl) -and ![string]::IsNullOrWhiteSpace($temporarySigningCertificatePath)) {
        $CodeSigningTimestampUrl = 'http://timestamp.digicert.com'
    }

    Sign-File -FilePath $artifactPath -CertificatePath $temporarySigningCertificatePath -CertificatePassword $CodeSigningCertificatePassword -TimestampUrl $CodeSigningTimestampUrl

    Write-Host "Copied launcher artifact to $artifactPath"
}
finally {
    if (![string]::IsNullOrWhiteSpace($temporarySigningCertificatePath) -and [string]::IsNullOrWhiteSpace($CodeSigningCertificatePath) -and (Test-Path $temporarySigningCertificatePath)) {
        Remove-Item $temporarySigningCertificatePath -Force
    }

    if ($null -ne $originalAssemblyInfo -and (Test-Path $assemblyInfoPath)) {
        [System.IO.File]::WriteAllText($assemblyInfoPath, $originalAssemblyInfo)
    }
}
param(
    [Parameter(Mandatory = $true)]
    [string]$UpdateRoot,

    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$LaunchFile = 'Wow.exe',
    [string]$BreakingNewsUrl = 'https://updates.oldmanwarcraft.com/updates/release-notes.json'
)

$ErrorActionPreference = 'Stop'

$resolvedUpdateRoot = (Resolve-Path $UpdateRoot).Path.TrimEnd('\', '/')
$manifestPath = Join-Path $resolvedUpdateRoot 'manifest.xml'

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)

$writer = [System.Xml.XmlWriter]::Create($manifestPath, $settings)

try {
    $writer.WriteStartDocument()
    $writer.WriteStartElement('manifest')
    $writer.WriteElementString('version', $Version)
    $writer.WriteElementString('baseUrl', $BaseUrl)
    $writer.WriteElementString('launchFile', $LaunchFile)

    if (![string]::IsNullOrWhiteSpace($BreakingNewsUrl)) {
        $writer.WriteElementString('breakingNewsUrl', $BreakingNewsUrl)
    }

    $writer.WriteStartElement('files')

    Get-ChildItem -Path $resolvedUpdateRoot -File -Recurse |
        Where-Object { $_.Name -ne 'manifest.xml' } |
        Sort-Object FullName |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($resolvedUpdateRoot.Length + 1).Replace('\', '/')
            $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()

            $writer.WriteStartElement('file')
            $writer.WriteAttributeString('path', $relativePath)
            $writer.WriteAttributeString('size', $_.Length.ToString())
            $writer.WriteAttributeString('sha256', $hash)
            $writer.WriteEndElement()
        }

    $writer.WriteEndElement()
    $writer.WriteEndElement()
    $writer.WriteEndDocument()
}
finally {
    $writer.Flush()
    $writer.Dispose()
}

Write-Host "Manifest written to $manifestPath"

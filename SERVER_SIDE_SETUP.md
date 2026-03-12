# WoW 3.3.5a Update Server Development Guide

## Overview

The current launcher does not require a dynamic API.
It can download updates from any HTTP/HTTPS server that exposes:

- a `manifest.xml` file
- optional realm news content through `breakingNewsUrl` or `news`
- the update files referenced by that manifest

This means the server side can be hosted with:

- IIS
- Nginx
- Apache
- GitHub Pages or raw file hosting
- CDN-backed object storage such as S3-compatible storage, Backblaze, Cloudflare R2, or Azure Blob Storage

## Recommended Repository Layout

```text
updates/
├─ manifest.xml
├─ Wow.exe
├─ Data/
│  ├─ patch-A.MPQ
│  ├─ patch-B.MPQ
│  └─ enUS/
│     └─ locale-enUS.MPQ
├─ Interface/
│  └─ AddOns/
│     └─ YourAddon/
│        └─ YourAddon.toc
└─ realmlist.wtf
```

The launcher can be pointed to:

- `https://your-domain.example/updates/manifest.xml`

## Manifest Format

The launcher currently expects XML in this format:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest>
  <version>3.3.5a.001</version>
  <baseUrl>https://your-domain.example/updates/</baseUrl>
  <launchFile>Wow.exe</launchFile>
  <breakingNewsUrl>https://your-domain.example/updates/news.txt</breakingNewsUrl>
  <files>
    <file path="Wow.exe" size="12345678" sha256="REPLACE_WITH_SHA256" />
    <file path="realmlist.wtf" size="55" sha256="REPLACE_WITH_SHA256" />
    <file path="Data/patch-A.MPQ" size="456789123" sha256="REPLACE_WITH_SHA256" />
    <file path="Data/enUS/locale-enUS.MPQ" size="789123456" sha256="REPLACE_WITH_SHA256" />
  </files>
</manifest>
```

## Manifest Rules

- `version` is displayed by the launcher
- `baseUrl` is optional if the files are in the same directory as `manifest.xml`
- `launchFile` is optional; if omitted, the launcher defaults to `Wow.exe`
- `breakingNewsUrl` is optional and should point to a JSON release-notes feed
- `news` is optional and can contain inline news text directly in the manifest
- `path` must be relative
- `size` should match the exact file size in bytes
- `sha256` should be uppercase or lowercase hexadecimal

## Realm News

The launcher can show news in two ways:

### Option 1: Remote JSON release-notes feed

Add this to the manifest:

```xml
<breakingNewsUrl>https://your-domain.example/updates/release-notes.json</breakingNewsUrl>
```

Example `release-notes.json`:

```json
{
  "generatedAt": "2026-03-12T21:11:21.451Z",
  "latest": {
    "version": "3.3.5a.011",
    "created_by": "scarecrow",
    "created_at": "2026-03-12 21:11:21",
    "release_notes": "Republished after runtime reload to include breakingNewsUrl in manifest."
  },
  "history": [
    {
      "version": "3.3.5a.011",
      "created_by": "scarecrow",
      "created_at": "2026-03-12 21:11:21",
      "release_notes": "Republished after runtime reload to include breakingNewsUrl in manifest."
    },
    {
      "version": "3.3.5a.010",
      "created_by": "scarecrow",
      "created_at": "2026-03-12 21:10:12",
      "release_notes": "Added breakingNewsUrl entry to manifest.xml for launcher news discovery."
    }
  ]
}
```

### Option 2: Inline news inside the manifest

```xml
<news>Season 1 is now live. Arena rewards have been updated.</news>
```

Use `breakingNewsUrl` when you want to publish breaking news and recent release history without rebuilding the full manifest.

## Launcher Self-Updates

The launcher executable now updates itself from the public GitLab project:

- `https://gitlab.thecorehosting.net/root/wow-launcher`

Expected release setup:

- publish a new Git tag such as `v1.0.1`
- create a GitLab release for that tag
- attach a release asset link named `Wow-Launcher.exe`

The launcher checks the latest public release through the GitLab API and downloads the asset named `Wow-Launcher.exe` when the release tag version is newer than the currently running launcher version.

## GitLab CI Release Automation

This workspace now includes:

- `.gitlab-ci.yml`
- `scripts/Build-Launcher.ps1`
- `scripts/Publish-GitLabRelease.ps1`

The pipeline is designed for a Windows GitLab runner and runs only for tags.

Required GitLab setup:

1. Create a Windows runner and tag it with `windows`.
2. Add a protected CI/CD variable named `GITLAB_API_TOKEN`.
3. Use a token with permission to create releases in the project.

Pipeline behavior:

- builds `Wow-Launcher.exe` in `Release`
- stores the executable as a job artifact
- creates a GitLab release for the tag
- adds a release asset link named `Wow-Launcher.exe`

Suggested tag format:

- `v1.0.0.0`
- `v1.0.1.0`

The launcher strips the leading `v` before comparing versions, so the release tag should still map cleanly to the assembly version.

## Server Requirements

The server must:

- allow anonymous HTTP/HTTPS GET requests
- serve files without HTML wrappers
- preserve exact file bytes
- support large file downloads
- return `200 OK` for valid files
- return `404` for missing files

Recommended:

- use HTTPS only
- disable directory browsing in production
- set long cache headers for patch files
- set shorter cache headers for `manifest.xml`

## Example IIS Setup

1. Install IIS.
2. Create a site or virtual directory pointing to your `updates` folder.
3. Ensure static content is enabled.
4. Bind HTTPS.
5. Confirm `manifest.xml` opens directly in a browser.

Suggested MIME types if needed:

- `.xml` -> `application/xml`
- `.wtf` -> `text/plain`
- `.mpq` -> `application/octet-stream`
- `.exe` -> `application/octet-stream`

## Example Nginx Setup

```nginx
server {
    listen 443 ssl;
    server_name updates.example.com;

    ssl_certificate     /etc/ssl/certs/fullchain.pem;
    ssl_certificate_key /etc/ssl/private/privkey.pem;

    root /var/www/wow-updates;

    location / {
        autoindex off;
        try_files $uri =404;
    }

    location = /manifest.xml {
        add_header Cache-Control "no-cache, no-store, must-revalidate";
    }

    location /Data/ {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
}
```

## Publishing Workflow

A simple release workflow:

1. Prepare the client files to distribute.
2. Copy only the files that should be updated into the `updates` folder.
3. Generate SHA256 hashes and file sizes.
4. Build `manifest.xml` and update `release-notes.json` if you use external news.
5. Upload files and manifest.
6. Publish a GitLab release with the latest `Wow-Launcher.exe` asset when the launcher itself changes.
7. Test with a clean local WoW client and an older launcher build.
8. Publish the new manifest URL to users.

## PowerShell Manifest Generator

Use this script to generate `manifest.xml` from a local update folder.

```powershell
param(
    [Parameter(Mandatory = $true)]
    [string]$UpdateRoot,

    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$LaunchFile = "Wow.exe"
)

$updateRoot = (Resolve-Path $UpdateRoot).Path.TrimEnd('\\')
$outFile = Join-Path $updateRoot 'manifest.xml'

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)

$writer = [System.Xml.XmlWriter]::Create($outFile, $settings)
$writer.WriteStartDocument()
$writer.WriteStartElement('manifest')

$writer.WriteElementString('version', $Version)
$writer.WriteElementString('baseUrl', $BaseUrl)
$writer.WriteElementString('launchFile', $LaunchFile)
$writer.WriteStartElement('files')

Get-ChildItem -Path $updateRoot -File -Recurse |
    Where-Object { $_.Name -ne 'manifest.xml' } |
    ForEach-Object {
        $relativePath = $_.FullName.Substring($updateRoot.Length + 1).Replace('\\', '/')
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
$writer.Flush()
$writer.Dispose()

Write-Host "Manifest written to $outFile"
```

Example usage:

```powershell
.\Generate-Manifest.ps1 -UpdateRoot "C:\build\wow-updates" -BaseUrl "https://updates.example.com/" -Version "3.3.5a.001"
```

## Versioning Strategy

Suggested versions:

- `3.3.5a.001`
- `3.3.5a.002`
- `2025.04.01`
- `2025.04.01-hotfix1`

Keep versions simple and sortable.

## Security Notes

- Never allow manifest paths like `../file`
- Only publish files intended for the client
- Prefer HTTPS to prevent tampering in transit
- Use SHA256 for integrity verification
- Keep the update host separate from internal admin systems

## Testing Checklist

Before release, verify:

- `manifest.xml` downloads in a browser
- every manifest file URL resolves correctly
- sizes match the hosted files
- hashes match the hosted files
- the GitLab launcher release downloads and restarts into the new version
- missing local files are downloaded
- changed local files are replaced
- unchanged files are skipped
- `Wow.exe` launches after update

## Optional Future Improvements

Possible server-side upgrades later:

- signed manifests
- delta patch generation
- channel support such as `stable`, `test`, and `ptr`
- per-region manifests
- CDN invalidation automation
- admin release dashboard
- release notes endpoint

## Minimal Production Recommendation

For a first version, use:

- a folder named `updates`
- HTTPS static hosting
- one `manifest.xml`
- one PowerShell script to generate the manifest
- manual upload for releases

That is enough for the current launcher implementation.

# Server-Side Setup for Old Man Warcraft Launcher

This document covers the current update and release setup used by `Old Man Warcraft Launcher`.

It reflects the current launcher behavior:

- the launcher always uses the hardcoded manifest URL `https://updates.oldmanwarcraft.com/updates/manifest.xml`
- the launcher news panel reads `breakingNewsUrl` from the manifest
- the expected news feed URL is `https://updates.oldmanwarcraft.com/updates/release-notes.json`
- launcher self-updates come from the latest GitLab release asset named `Wow-Launcher.exe`

## Update System Overview

There are two update flows:

1. **Client content updates** are delivered by `manifest.xml` plus hosted patch files.
2. **Launcher executable updates** are delivered by GitLab releases.

## Expected Update Host Layout

A typical hosted update folder should look like this:

```text
updates/
  manifest.xml
  release-notes.json
  Wow.exe
  Data/
  Interface/
  WTF/
```

Only publish files that should actually be distributed to players.

## Manifest Format

The launcher expects XML like this:

```xml
<manifest>
  <version>3.3.5a.001</version>
  <baseUrl>https://updates.oldmanwarcraft.com/updates/</baseUrl>
  <launchFile>Wow.exe</launchFile>
  <breakingNewsUrl>https://updates.oldmanwarcraft.com/updates/release-notes.json</breakingNewsUrl>
  <files>
    <file path="Wow.exe" size="123456" sha256="..." />
    <file path="Data/patch-oldman.MPQ" size="987654321" sha256="..." />
  </files>
</manifest>
```

### Manifest rules

- `version` is displayed in the launcher UI
- `baseUrl` is the base download location for all file entries
- `launchFile` should point to the game executable, usually `Wow.exe`
- `breakingNewsUrl` should point to the JSON release notes feed
- each file entry must use a relative path
- each file entry should include `size` and `sha256`

## Manifest Generation Script

Use the built-in script instead of manually writing the manifest:

```powershell
.\scripts\Generate-Manifest.ps1 `
    -UpdateRoot "C:\build\wow-updates" `
    -BaseUrl "https://updates.oldmanwarcraft.com/updates/" `
    -Version "3.3.5a.001"
```

Optional parameters:

- `-LaunchFile "Wow.exe"`
- `-BreakingNewsUrl "https://updates.oldmanwarcraft.com/updates/release-notes.json"`

What the script does:

- scans all files under the update root
- skips `manifest.xml`
- writes file paths relative to the update root
- computes SHA256 hashes
- writes file sizes
- includes the configured `breakingNewsUrl`

## Release Notes Feed

The launcher news section supports a JSON feed like this:

```json
{
  "generatedAt": "2026-03-12T21:11:21Z",
  "latest": {
    "version": "3.3.5a.011",
    "created_by": "scarecrow",
    "created_at": "2026-03-12 21:11:21",
    "release_notes": "Launcher polish, manifest refresh, and release automation updates."
  },
  "history": [
    {
      "version": "3.3.5a.011",
      "created_by": "scarecrow",
      "created_at": "2026-03-12 21:11:21",
      "release_notes": "Launcher polish, manifest refresh, and release automation updates."
    },
    {
      "version": "3.3.5a.010",
      "created_by": "scarecrow",
      "created_at": "2026-03-12 21:10:12",
      "release_notes": "Added manifest-driven release notes support."
    }
  ]
}
```

The launcher can also fall back to plain text if the feed does not deserialize as JSON, but JSON is the preferred format.

## Launcher Self-Update Requirements

The launcher checks the latest public GitLab release using the GitLab API and looks for a release asset named:

- `Wow-Launcher.exe`

Expected release shape:

- Git tag such as `v1.0.1`
- GitLab release for that tag
- release asset link named `Wow-Launcher.exe`

The launcher strips the leading `v` from the tag before comparing versions.

## GitLab CI/CD Release Automation

The project now includes:

- `.gitlab-ci.yml`
- `scripts/Build-Launcher.ps1`
- `scripts/New-GitLabTag.ps1`
- `scripts/Publish-GitLabRelease.ps1`

### Pipeline stages

1. `verify`
2. `tag`
3. `build`
4. `release`

### Pipeline behavior

- merge requests run the validation build
- default-branch pushes run validation and automatic tag creation
- tag pipelines build the launcher, stamp the assembly version, and publish the GitLab release

### Required GitLab setup

1. Create a Windows runner with the tag `windows`.
2. Make sure `MSBuild.exe` is available on that runner.
3. Add a protected CI/CD variable named `GITLAB_API_TOKEN`.
4. Use a token with API scope.

### Skip automatic tagging

To skip auto-tagging for a specific default-branch commit, add this to the commit message:

- `[skip release]`

## Publishing Workflow

### Launcher executable

1. Push the launcher changes to the default branch.
2. Let the `verify` job pass.
3. Let the `tag` job create the next `vX.Y.Z` launcher tag.
4. Let the tag pipeline publish the GitLab release.
5. Confirm the release contains `Wow-Launcher.exe`.
6. Test launcher self-update from an older launcher build.

### Client content

1. Prepare the updated WoW client files.
2. Copy only the intended distributable files into the hosted update folder.
3. Update `release-notes.json`.
4. Run `scripts/Generate-Manifest.ps1`.
5. Upload the patch files, `manifest.xml`, and `release-notes.json`.
6. Test with a clean or outdated client folder.
7. Confirm missing files download, changed files replace correctly, and unchanged files are skipped.

## Server Requirements

The update host must:

- allow anonymous HTTP or HTTPS GET requests
- return raw files without HTML wrappers
- preserve exact file bytes
- support large downloads
- return `200 OK` for valid files
- return `404` for missing files

Recommended:

- use HTTPS
- disable directory browsing in production
- use short cache lifetimes for `manifest.xml`
- use longer cache lifetimes for static patch files

## Example IIS Notes

- enable Static Content
- serve `manifest.xml` directly
- bind HTTPS
- verify MIME types if required

Common MIME types:

- `.xml` -> `application/xml`
- `.wtf` -> `text/plain`
- `.mpq` -> `application/octet-stream`
- `.exe` -> `application/octet-stream`

## Example Nginx Configuration

```nginx
server {
    listen 443 ssl;
    server_name updates.oldmanwarcraft.com;

    ssl_certificate     /etc/ssl/certs/fullchain.pem;
    ssl_certificate_key /etc/ssl/private/privkey.pem;

    root /var/www/oldmanwarcraft-updates;

    location / {
        autoindex off;
        try_files $uri =404;
    }

    location = /updates/manifest.xml {
        add_header Cache-Control "no-cache, no-store, must-revalidate";
    }

    location /updates/Data/ {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }
}
```

## Security Notes

- never publish manifest paths containing `..`
- only upload files intended for the client
- prefer HTTPS for all update endpoints
- rely on SHA256 verification for file integrity
- keep update hosting separate from internal admin services when possible

## Release Checklist

Before publishing, verify:

- `manifest.xml` opens directly in a browser
- every file listed in the manifest is downloadable
- hosted file sizes match the manifest
- hosted file hashes match the manifest
- the launcher news feed loads from `release-notes.json`
- the GitLab release exposes `Wow-Launcher.exe`
- launcher self-update works from an older launcher build
- the client launches successfully after patching

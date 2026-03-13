# Old Man Warcraft Launcher

`Old Man Warcraft Launcher` is a Windows Forms launcher for the `Old Man Warcraft` Wrath of the Lich King `3.3.5a` client.

It targets `.NET Framework 4.7.2` and provides:

- client file update checks from the hardcoded manifest URL `https://updates.oldmanwarcraft.com/updates/manifest.xml`
- launcher self-updates from the latest GitHub release
- a built-in news panel powered by the manifest `breakingNewsUrl` feed
- GitHub Actions automation for validation, tagging, release publishing, and launcher delivery

## Project Structure

- `Wow-Launcher/` - Windows Forms application
- `scripts/Build-Launcher.ps1` - local and CI build script
- `scripts/New-GitHubTag.ps1` - automatic GitHub tag creation script
- `scripts/Generate-Manifest.ps1` - client update manifest generator
- `.github/workflows/` - GitHub Actions CI/CD workflows
- `SERVER_SIDE_SETUP.md` - update hosting and deployment guide

## Runtime Update Architecture

### Client updates

The launcher always checks the same hardcoded manifest URL:

- `https://updates.oldmanwarcraft.com/updates/manifest.xml`

The manifest controls:

- the visible client build number
- the list of files to download
- SHA256 validation
- file size validation
- the launch target, usually `Wow.exe`
- the `breakingNewsUrl` for launcher news

### Launcher self-updates

The launcher checks the latest GitHub release API for a release asset named:

- `Wow-Launcher.exe`

When the release tag version is newer than the running assembly version, the launcher downloads the updated executable, restarts, and replaces itself.

## Prerequisites

- Windows
- Visual Studio or Build Tools with `MSBuild.exe`
- `.NET Framework 4.7.2` targeting pack
- GitHub Actions enabled for the repository

## Local Development

### Build locally

```powershell
.\scripts\Build-Launcher.ps1
```

### Build a version-stamped release locally

```powershell
.\scripts\Build-Launcher.ps1 -ReleaseVersion v1.0.1
```

This temporarily stamps `AssemblyVersion`, `AssemblyFileVersion`, and `AssemblyInformationalVersion` during the build.

## Manifest Generation

Generate `manifest.xml` from a prepared update folder:

```powershell
.\scripts\Generate-Manifest.ps1 `
    -UpdateRoot "C:\build\wow-updates" `
    -BaseUrl "https://updates.oldmanwarcraft.com/updates/" `
    -Version "3.3.5a.001"
```

By default, the manifest generator also writes:

- `breakingNewsUrl` = `https://updates.oldmanwarcraft.com/updates/release-notes.json`

It excludes launcher metadata files such as `manifest.xml`, `release-notes.json`, hidden files, system files, and dot-prefixed paths.

## News Feed Format

The launcher expects `breakingNewsUrl` to point to a JSON feed containing `latest` and optional `history` entries.

Typical feed URL:

- `https://updates.oldmanwarcraft.com/updates/release-notes.json`

See `SERVER_SIDE_SETUP.md` for a complete example.

## GitHub Actions CI/CD

The project uses these workflows:

1. `CI`
2. `Create Release Tag`
3. `Release`

### Pipeline behavior

- pull requests run the validation build
- pushes to `main` run validation and then create the next launcher tag
- tag pushes build the launcher with the tag version and publish the GitHub release

### Automatic tagging

Default-branch pushes create the next patch tag in this format:

- `v1.0.1`
- `v1.0.2`
- `v1.0.3`

To skip automatic tag creation for a commit, include this in the commit message:

- `[skip release]`

### Target GitHub repository

The repository is configured for GitHub releases at:

- `https://github.com/scarecr0w12/OldManWow-Launcher`

## Release Process

### Launcher release

1. Push changes to `main`.
2. Let the `CI` workflow succeed.
3. Let the `Create Release Tag` workflow create the next `vX.Y.Z` tag.
4. The tag workflow builds `Wow-Launcher.exe`.
5. The `Release` workflow creates the GitHub release and attaches the launcher executable.
6. The launcher will detect the newer release automatically.

### Client patch release

1. Prepare the files to distribute in an update folder.
2. Run `scripts/Generate-Manifest.ps1`.
3. Upload the files, `manifest.xml`, and `release-notes.json`.
4. Test with an older client folder.
5. Confirm the launcher news panel and patch download flow both work.

## Additional Documentation

- `SERVER_SIDE_SETUP.md` - hosting, manifest, release notes, caching, and update deployment details

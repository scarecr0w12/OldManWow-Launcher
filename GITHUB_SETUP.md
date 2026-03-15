# GitHub Repository and CI/CD Setup

This repository now includes GitHub Actions workflows for validation builds, automatic release tagging, and GitHub Releases publishing.

## Included workflows

- `.github/workflows/ci.yml`
  - runs on every push and pull request
  - builds the launcher on `windows-latest`
  - uploads `artifacts/Wow-Launcher.exe` as a workflow artifact

- `.github/workflows/create-release-tag.yml`
  - runs on pushes to `main`
  - creates the next patch tag in the format `vX.Y.Z`
  - dispatches the release workflow for the created tag
  - skips tagging when the commit message contains `[skip release]`

- `.github/workflows/release.yml`
  - runs when a `v*.*.*` tag is pushed or when dispatched by the tag workflow
  - stamps the launcher assembly version from the tag
  - builds `Wow-Launcher.exe`
  - publishes a GitHub Release with the launcher attached

## One-time GitHub repository creation

Create a new empty GitHub repository, then connect this workspace to it.

Example:

```powershell
git remote add github https://github.com/scarecr0w12/OldManWow-Launcher.git
git push -u github main
```

If this repository already has an `origin` remote that should point to GitHub instead, update it before pushing.

## Required GitHub settings

### Actions permissions

In the GitHub repository settings:

- enable GitHub Actions
- allow workflows to create and approve pull requests if your org policy requires it
- ensure the default `GITHUB_TOKEN` has `Read and write permissions` for repository contents

The release-tag workflow requires `contents: write` so it can create tags.

### Release signing secrets

To reduce Windows `Unknown publisher` and SmartScreen warnings for public downloads, add these repository or environment secrets:

- `CODE_SIGNING_CERTIFICATE_BASE64` - base64-encoded `.pfx` certificate
- `CODE_SIGNING_CERTIFICATE_PASSWORD` - optional `.pfx` password
- `CODE_SIGNING_TIMESTAMP_URL` - optional timestamp URL

The release workflow passes these secrets into `scripts/Build-Launcher.ps1`, which signs `artifacts/Wow-Launcher.exe` before publishing the release.

## Launcher self-updater configuration

Set the target GitHub repository in `Wow-Launcher/App.config`:

```xml
<add key="LauncherGitHubRepository" value="scarecr0w12/OldManWow-Launcher" />
```

This points the launcher self-updater at the GitHub repository that will host launcher releases.

Optional override:

```xml
<add key="LauncherGitHubApiBaseUrl" value="https://api.github.com" />
```

The launcher now checks GitHub Releases at `releases/latest` and downloads the `Wow-Launcher.exe` asset from the newest published release.

## Release flow

1. Push changes to `main`.
2. `CI` validates the Windows build.
3. `Create Release Tag` creates the next patch tag unless the commit message contains `[skip release]`.
4. `Create Release Tag` dispatches `Release` for that tag.
5. `Release` builds `Wow-Launcher.exe` and publishes it to GitHub Releases.

## Notes

- The launcher artifact name remains `Wow-Launcher.exe`.
- The existing local build script remains the source of truth for CI and release builds.

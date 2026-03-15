# Copilot Instructions

## General Instructions
- Treat this workspace as a brand new project and brand new git repository; do not rely on or discuss prior repo history unless explicitly asked.
- Verify configured Git remotes before stating which remotes exist; this workspace is expected to use only a GitHub remote.
- Do not assume the active Git remote/repository owner is relevant to the task unless explicitly stated by the user.
- Avoid repeated progress/status messages when diagnosing a remote workflow failure; give a concise result and next action instead of looping on the same update.

## Project Guidelines
- User prefers the launcher UI to have a more World of Warcraft-inspired visual style and include a news section.
- The launcher should use a hardcoded manifest URL: https://updates.oldmanwarcraft.com/updates/manifest.xml. Do not show the update source in the UI because the launcher always uses the same hardcoded manifest URL.
- The news section of the launcher is expected to source its content from the manifest entry `breakingNewsUrl`, which now points to a JSON release notes feed at `https://updates.oldmanwarcraft.com/updates/release-notes.json`, containing breaking news and update history.
- The launcher should use the server name 'Old Man Warcraft' in its UI branding.
- The launcher UI should show online player count and realm status directly on the form.
- When implementing update logic, avoid deleting existing files during replacement as it is considered slower; prefer non-delete replacement strategies when possible.
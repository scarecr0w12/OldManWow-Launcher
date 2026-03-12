# Copilot Instructions

## General Instructions
- Treat this workspace as a brand new project and brand new git repository; do not rely on or discuss prior repo history unless explicitly asked.
- Do not assume the active Git remote/repository owner is relevant to the task unless explicitly stated by the user.


## Project Guidelines
- User prefers the launcher UI to have a more World of Warcraft-inspired visual style and include a news section.
- The launcher should use a hardcoded manifest URL: https://updates.oldmanwarcraft.com/updates/manifest.xml. Do not show the update source in the UI because the launcher always uses the same hardcoded manifest URL.
- The news section of the launcher is expected to source its content from the manifest entry `breakingNewsUrl`, which now points to a JSON release notes feed at `https://updates.oldmanwarcraft.com/updates/release-notes.json`, containing breaking news and update history.
- The launcher should use the server name 'Old Man Warcraft' in its UI branding.
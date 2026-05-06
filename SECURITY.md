# Security

Incursa.OpenAI.Codex launches the local Codex runtime as a subprocess. Treat the application using this SDK as the access boundary for local files, local credentials, and any repositories exposed to Codex.

## Required Controls

- Install and authenticate Codex using the account and permissions intended for the host application.
- Use the narrowest practical working directory for automated runs.
- Review Codex sandbox and approval settings before using this SDK on sensitive repositories.
- Keep `CodexClientOptions.ApiKey`, environment variables, and any local Codex auth material out of source control.
- Do not publish sample output, logs, or screenshots that expose private prompts, private repository paths, credentials, or transcripts.
- Treat generated patches, command output, and streamed events as potentially sensitive application data.

## Reporting

Do not report secrets, exploit details, private transcripts, or local credential paths in a public issue.

Use GitHub private vulnerability reporting if it is enabled for the repository. If it is unavailable, contact security@incursa.com.

For general open-source project questions, contact oss@incursa.com.

For unreleased builds, only the latest `main` branch or current release candidate is expected to receive security fixes.

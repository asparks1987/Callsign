# Callsign Support

Callsign is in Alpha. Support should focus on reproducible diagnostics, safe recovery, and honest scope rather than promising unattended reliability.

## Before asking for help

Read:

- [Getting started](docs/guides/GETTING_STARTED.md)
- [Troubleshooting](docs/guides/TROUBLESHOOTING.md)
- [Logs and observability](docs/reference/OBSERVABILITY.md)
- [Known open questions](docs/reference/OPEN_QUESTIONS.md)

Collect:

- Callsign version or commit.
- Windows version and architecture.
- Installation mode.
- Whether the UI and background runtime are running.
- The visible session state.
- Sanitized error codes and log excerpts.
- Exact reproduction steps.
- Whether the problem occurs with a clean local profile.

Do not attach raw voice samples, unredacted transcripts, screenshots containing private data, or logs containing tokens and personal paths.

## Where to report

- Bugs: repository issue tracker using the bug template.
- Feature proposals: feature template plus target release and safety analysis.
- Security issues: private advisory; see [SECURITY.md](SECURITY.md).
- Documentation problems: docs issue with the conflicting paths quoted.

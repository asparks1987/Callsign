# Getting Started

## Audience

Alpha testers and contributors using Windows.

## Prerequisites

Confirm the current repository's exact requirements. The documented baseline expects:

- Windows.
- PowerShell.
- A compatible .NET SDK/runtime.
- Python for site generation and packaged voice helpers.
- A working microphone.
- Permission to run the installer or per-user runtime.
- A non-sensitive Windows account or VM for automation testing.

## Build

From the repository root:

```powershell
.\buildcallsign.ps1
```

Expected documented outputs include setup, portable/run, and service executables. Inspect the current build manifest rather than assuming names alone prove a complete payload.

## Smoke test

```powershell
dotnet run --project tests/Callsign.AlphaSmoke/Callsign.AlphaSmoke.csproj
```

Optional documented modes may include voice-listener and live-launch checks. Check the current test program help before use.

## Install

1. Close running Callsign processes.
2. Run the setup executable.
3. Confirm the desktop shortcut.
4. Open the configuration manager.
5. Confirm the UI reports the installed runtime mode and version.

Use a test Windows profile until security and privacy review are complete.

## Create a profile

1. Choose a callsign.
2. Enter only the minimum optional profile data.
3. Save.
4. Confirm the profile remains after restart.
5. Do not use a callsign that reveals a secret.

## Enroll voice

1. Select and test the microphone.
2. Record the required fresh samples.
3. Replay each sample.
4. Retry silence, clipping, or noisy captures.
5. Review whether raw samples are retained.
6. Activate enrollment.

## Run the Alpha flow

1. Confirm runtime health.
2. Say `Callsign`.
3. Confirm the overlay appears.
4. Say the active callsign.
5. Confirm the identity state.
6. Say `Open Notepad`.
7. Watch the visible launch.
8. Test `stop` and `cancel`.

## Safety checks

Verify:

- Wake alone does nothing.
- Transcript-like wake text does not bypass the detector.
- Wrong callsign blocks action.
- Paths, URLs, and shell-like requests are rejected.
- Missing/ambiguous apps fail safely.
- Overlay failure does not become hidden execution.
- Logs contain no raw audio or sensitive transcript.

## Next steps

- [Manual Alpha walkthrough](MANUAL_ALPHA_WALKTHROUGH.md)
- [Troubleshooting](TROUBLESHOOTING.md)
- [Test plan](../reference/TEST_PLAN.md)

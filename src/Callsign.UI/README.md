# Callsign Setup App

This folder contains the alpha setup and monitoring experience for Callsign.

## Run

- Open in Visual Studio or dotnet CLI.
- Run `dotnet run --project src/Callsign.UI/Callsign.UI.csproj`.

## What it does

- Create and manage user accounts.
- Activate and reset voice control for the selected callsign.
- Run the visible wake word plus identity plus command session flow.
- Show the `callsign.gif` wake overlay and live transcript readout while speaking, including confidence when available.
- Launch an installed app through the Start menu search experience.
- Expose follow-on alpha surfaces for dictation, browser control, and file search.

## Profile storage

User profiles and settings are stored under:

%LOCALAPPDATA%\Callsign\Profiles\<callsign>\settings.json

Alpha launch audit events are appended beside the profile:

%LOCALAPPDATA%\Callsign\Profiles\<callsign>\alpha-audit.jsonl

The alpha audit file records only the callsign, requested app name, timestamp, and `start_menu_search` launch path. It does not store microphone audio, screenshots, or full speech transcripts.

## App surfaces

- Account tab: create, edit, and delete accounts.
- Voice tab: rehearse sample phrases and activate voice control for the selected callsign.
- Session tab: wake, verify identity, capture the command, and launch the app.
- Getting Started: reopen the walkthrough from the Account tab.
- Storage path label: shows exactly where each callsign profile is stored.

## Alpha v1 MVP happy path

1. Open Callsign.
2. Create an account with a speakable callsign, such as `Alpha` or `Aryn One`; prefer spoken words over digits.
3. Select the Session tab and choose `Start Listening`; Callsign activates voice for the saved callsign if needed.
4. Confirm the listener is running.
5. Say `Callsign Alpha open Notepad`, replacing `Alpha` with the saved callsign.
6. Callsign should show the wake overlay, show the recognized identity and command, verify the callsign, and open the requested app through Start search.
7. The overlay should stay visible until the voice session completes, cancels, times out, or locks out.
8. The Dictation, Browser, and Files tabs are follow-on alpha surfaces, not the `v1.0` launch MVP.
9. Getting Started reopens the walkthrough from the Account tab if you want the setup flow again.

If microphone recognition is unavailable, use `Launch test phrase` and `Test Phrase Launch` with the same phrase to exercise the same alpha state machine and Start menu launcher. This is a real launch test and can open the requested app. Passing this test proves the account, callsign, parser, policy boundary, audit, and Start menu launch path; it does not prove microphone recognition. You can also test the two-step flow by first entering `Callsign Alpha`, then entering `Notepad` after identity is verified.

Alpha v1 accepts installed app names only. It intentionally rejects paths, URLs, terminal commands, and shell-style payloads.

## Alpha verification checklist

The UI alpha path is implemented, but it is not considered ready until this checklist passes on a Windows desktop.

To print the current manual checklist and local proof paths, run:

```powershell
.\scripts\alpha_v1_checklist.ps1 -Callsign Alpha -AppName Notepad
```

Before calling the UI alpha ready, verify:

1. `dotnet build src/Callsign.UI/Callsign.UI.csproj` succeeds.
2. Callsign opens without a startup error dialog.
3. A new account can be created with a callsign such as `Alpha`.
4. `Start Listening` activates voice for the saved callsign if needed.
5. `Launch test phrase` with `Callsign Alpha open Notepad` launches Notepad through Start search.
6. Microphone input with `Callsign Alpha open Notepad` launches Notepad through Start search.
7. The two-step flow works: `Callsign Alpha`, then `Notepad`.
8. The wake overlay appears on wake and shows the live readout below `callsign.gif`, plus a transcript confidence cue when Callsign has one.
9. `stop listening` stops the listener without launching anything.
10. Path, URL, WSL, terminal, and shell-like command requests are rejected.
11. `%LOCALAPPDATA%\Callsign\Profiles\Alpha\alpha-audit.jsonl` records the Start menu launch event.
12. The Dictation, Browser, and Files tabs remain usable as follow-on alpha surfaces.
13. The Getting Started action reopens the walkthrough from the Account tab.

## Troubleshooting alpha launch

- Startup and runtime UI errors are written to `%LOCALAPPDATA%\Callsign\Logs\startup-error.log`.
- If microphone recognition does not start, confirm Windows has an installed speech recognizer and that Callsign has microphone access.
- If recognition is unreliable, use `Launch test phrase` first. If the test phrase works, the session and Start menu launcher are working and the issue is likely microphone or speech recognition setup.
- If Start search opens but the wrong app launches, try a simpler installed app name such as `Notepad`, `Calculator`, or `Paint`.
- If no app should launch, say `stop listening` or press `Cancel` before testing another phrase.


# Callsign Setup App

This folder contains the alpha setup experience for Callsign.

## Run

- Open in Visual Studio or dotnet CLI.
- Run `dotnet run --project src/Callsign.UI/Callsign.UI.csproj`.

## What it does

- Create and manage user accounts.
- Activate and reset voice control for the selected callsign.
- Run the visible wake word plus callsign session flow.
- Launch an installed app through the Start menu search experience.
- Dictate text into a visible editor and copy or paste the result.
- Open websites or search the web through the default browser.
- Search the intended local file scope and open matching results.

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
- Storage path label: shows exactly where each callsign profile is stored.

## Alpha v1 happy path

1. Open Callsign.
2. Create an account with a speakable callsign, such as `Alpha` or `Aryn One`; prefer spoken words over digits.
3. Select the Session tab and choose `Start Listening`; Callsign activates voice for the saved callsign if needed.
4. Confirm the listener is running.
5. Say `Callsign Alpha open Notepad`, replacing `Alpha` with the saved callsign.
6. Callsign should show the recognized phrase, verify the callsign, show the Start menu launch intent, and open the requested app through Start search.
7. Open the Dictation tab, choose `Start Dictation`, speak a short sentence, and verify the text appears in the visible dictation box.
8. Open the Browser tab, enter a URL or search phrase, and confirm the default browser opens the visible target.
9. Open the Files tab, search for a known file name, and confirm the result list shows matching files or a clear empty state.

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
8. `stop listening` stops the listener without launching anything.
9. Path, URL, WSL, terminal, and shell-like command requests are rejected.
10. `%LOCALAPPDATA%\Callsign\Profiles\Alpha\alpha-audit.jsonl` records the Start menu launch event.
11. The Dictation tab captures speech and exposes the text visibly.
12. The Browser tab opens a website or search query in the default browser.
13. The Files tab finds a known local file and opens it from the results list.

## Troubleshooting alpha launch

- Startup and runtime UI errors are written to `%LOCALAPPDATA%\Callsign\Logs\startup-error.log`.
- If microphone recognition does not start, confirm Windows has an installed speech recognizer and that Callsign has microphone access.
- If recognition is unreliable, use `Launch test phrase` first. If the test phrase works, the session and Start menu launcher are working and the issue is likely microphone or speech recognition setup.
- If Start search opens but the wrong app launches, try a simpler installed app name such as `Notepad`, `Calculator`, or `Paint`.
- If no app should launch, say `stop listening` or press `Cancel` before testing another phrase.


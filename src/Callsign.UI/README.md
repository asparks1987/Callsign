# Callsign Setup App

This folder contains the alpha setup experience for Callsign.

## Run

- Open in Visual Studio or dotnet CLI.
- Run `dotnet run --project src/Callsign.UI/Callsign.UI.csproj`.

## What it does

- Create and manage user accounts.
- Enroll and reset the voice identity profile.
- Run the visible wake word plus callsign session flow.
- Launch an installed app through the Start menu search experience.

## Profile storage

User profiles and settings are stored under:

%LOCALAPPDATA%\Callsign\Profiles\<callsign>\settings.json

## App surfaces

- Account tab: create, edit, and delete accounts.
- Voice tab: record samples and train the enrolled voice identity.
- Session tab: wake, verify identity, capture the command, and launch the app.
- Storage path label: shows exactly where each callsign profile is stored.



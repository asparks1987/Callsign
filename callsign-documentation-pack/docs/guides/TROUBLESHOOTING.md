# Troubleshooting

## Callsign does not start

Check:

- UI and runtime executable presence.
- Version compatibility.
- Installer log.
- Per-user data permissions.
- Whether an old process holds files.
- Service/fallback mode.

Do not repeatedly reinstall without preserving the first failure log.

## UI says runtime unavailable

- Confirm process/service state.
- Check authenticated IPC endpoint and version.
- Restart runtime from the visible control.
- Review structured error code.
- Reinstall if packaged payload hashes fail.

## No microphone

- Confirm Windows privacy permission.
- Select the correct device.
- Check whether another app has exclusive access.
- Disconnect/reconnect.
- Test input level.
- Restart runtime after device change.

## Wake does not trigger

- Confirm microphone activity.
- Confirm wake runtime/model present and hash-valid.
- Confirm correct model/version/format.
- Use the documented repair control.
- Inspect wake score telemetry, not raw audio.
- Do not enable transcript compatibility wake as a shortcut.

## Too many false wakes

- Confirm configured threshold is within tested bounds.
- Check device gain/noise.
- Capture sanitized numeric diagnostics.
- Recalibrate with approved fixtures.
- Do not raise/lower threshold blindly for release.

## Identity always fails

- Confirm active profile and enrollment.
- Check sample/model compatibility.
- Retry with clean audio.
- Distinguish low quality from mismatch.
- Reset and re-enroll if the model changed.
- Remember the gate is not OS authentication.

## Overlay missing

- Check overlay asset.
- Check UI/overlay process connection.
- Test DPI/multi-monitor.
- Cancel the session if no visible fallback exists.
- Do not allow hidden action.

## App does not open

- Confirm plain installed app name.
- Inspect ranked candidates.
- Reject path/URL/command input.
- Confirm Start menu availability.
- Check integrity boundary.
- Inspect verification result; input sent is not proof of launch.

## Logs

Use the UI shortcuts. Before sharing:

- Remove raw transcripts.
- Redact user paths and callsign.
- Exclude audio, embeddings, screenshots, clipboard, tokens, and unrelated files.
- Include correlation ID and component versions.

## Reset order

Use the least destructive:

1. Cancel session.
2. Restart runtime.
3. Rebuild app index.
4. Repair wake runtime.
5. Reset enrollment.
6. Reset profile.
7. Reinstall preserving data.
8. Remove all local data only after explicit confirmation.

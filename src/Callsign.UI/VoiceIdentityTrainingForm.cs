using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Media;
using Callsign.UI.Models;
using Callsign.UI.Services;
using NAudio.Wave;

namespace Callsign.UI;

public sealed class VoiceIdentityTrainingForm : Form
{
    private readonly ProfileStore _profileStore;
    private readonly UserProfile _profile;
    private readonly VoiceCommandService _voiceCommandService;
    private readonly VoiceSampleCaptureService _sampleCapture = new();
    private readonly VoiceBiometricVerificationService _biometricService = new();

    private readonly Label _statusLabel;
    private readonly Label _sampleLabel;
    private readonly Label _wakeSampleLabel;
    private readonly Label _qualityLabel;
    private readonly Label[] _sampleStatusLabels = new Label[3];
    private readonly ProgressBar _progress;
    private readonly Button _recordButton;
    private readonly Button _recordWakeButton;
    private readonly Button _playButton;
    private readonly Button _enrollButton;
    private readonly Button _calibrateButton;
    private readonly Button _wakeCalibrateButton;
    private readonly Button _repairRuntimeButton;
    private readonly Button _closeButton;
    private readonly System.Windows.Forms.Timer _levelTimer = new() { Interval = 100 };
    private bool _busy;
    private string? _currentSamplePath;
    private bool _recordingWakeSample;

    public VoiceIdentityTrainingForm(ProfileStore profileStore, UserProfile profile, VoiceCommandService voiceCommandService)
    {
        _profileStore = profileStore;
        _profile = profile;
        _voiceCommandService = voiceCommandService;

        Text = $"Train Voice Identity - {profile.Callsign}";
        Width = 820;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 12
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var heading = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = $"Fingerprint '{profile.Callsign}' to your voice"
        };
        layout.Controls.Add(heading, 0, 0);
        layout.SetColumnSpan(heading, 2);

        var prompt = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Multiline = true,
            Height = 72,
            Text = $"Say: Callsign {profile.Callsign}. Hold the record button while speaking, release to save the sample, then enroll the voice identity."
        };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Prompt" }, 0, 1);
        layout.Controls.Add(prompt, 1, 1);

        _progress = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = Math.Max(3, profile.Settings.VoiceSamplesRequired) };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Samples" }, 0, 2);
        layout.Controls.Add(_progress, 1, 2);

        _sampleLabel = new Label { AutoSize = true };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Progress" }, 0, 3);
        layout.Controls.Add(_sampleLabel, 1, 3);

        var sampleRows = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true
        };
        sampleRows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        sampleRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var index = 0; index < _sampleStatusLabels.Length; index++)
        {
            var sampleName = new Label { AutoSize = true, Text = $"Sample {index + 1}" };
            _sampleStatusLabels[index] = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(520, 0),
                Text = "Pending."
            };
            sampleRows.Controls.Add(sampleName, 0, index);
            sampleRows.Controls.Add(_sampleStatusLabels[index], 1, index);
        }
        layout.Controls.Add(new Label { AutoSize = true, Text = "Voice samples" }, 0, 4);
        layout.Controls.Add(sampleRows, 1, 4);

        _wakeSampleLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "No wake samples recorded yet." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Wake samples" }, 0, 5);
        layout.Controls.Add(_wakeSampleLabel, 1, 5);

        _qualityLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "No sample analyzed yet." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Audio quality" }, 0, 6);
        layout.Controls.Add(_qualityLabel, 1, 6);

        _statusLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "Ready to record." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Status" }, 0, 7);
        layout.Controls.Add(_statusLabel, 1, 7);

        _recordButton = new Button
        {
            Text = "REC Hold to Record",
            Width = 170,
            Height = 44,
            BackColor = Color.Firebrick,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _recordButton.FlatAppearance.BorderSize = 0;
        _recordButton.MouseDown += RecordButtonMouseDown;
        _recordButton.MouseUp += RecordButtonMouseUp;
        _recordButton.MouseLeave += (_, _) =>
        {
            if (_sampleCapture.IsRecording)
                _recordButton.Capture = true;
        };

        _recordWakeButton = new Button
        {
            Text = "REC Wake Sample",
            Width = 160,
            Height = 44,
            BackColor = Color.DarkOliveGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _recordWakeButton.FlatAppearance.BorderSize = 0;
        _recordWakeButton.MouseDown += RecordWakeButtonMouseDown;
        _recordWakeButton.MouseUp += RecordWakeButtonMouseUp;
        _recordWakeButton.MouseLeave += (_, _) =>
        {
            if (_sampleCapture.IsRecording)
                _recordWakeButton.Capture = true;
        };

        _playButton = new Button { Text = "Play Sample", Width = 120, Height = 44 };
        _playButton.Click += (_, _) => PlayLatestSample();

        _enrollButton = new Button { Text = "Enroll Voice Identity", Width = 170, Height = 44 };
        _enrollButton.Click += async (_, _) => await EnrollIdentityAsync();

        _calibrateButton = new Button { Text = "Calibrate Mic", Width = 130, Height = 44 };
        _calibrateButton.Click += (_, _) => CalibrateMicrophone();

        _wakeCalibrateButton = new Button { Text = "Calibrate Wakeword", Width = 180, Height = 44 };
        _wakeCalibrateButton.Click += async (_, _) => await CalibrateWakewordAsync();

        _repairRuntimeButton = new Button { Text = "Repair Identity Runtime", Width = 180, Height = 44 };
        _repairRuntimeButton.Click += (_, _) => RepairIdentityRuntime();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_recordButton);
        buttons.Controls.Add(_recordWakeButton);
        buttons.Controls.Add(_playButton);
        buttons.Controls.Add(_enrollButton);
        buttons.Controls.Add(_calibrateButton);
        buttons.Controls.Add(_wakeCalibrateButton);
        buttons.Controls.Add(_repairRuntimeButton);
        layout.Controls.Add(buttons, 1, 8);

        _closeButton = new Button { Text = "Close", Width = 100 };
        _closeButton.Click += (_, _) => Close();
        layout.Controls.Add(_closeButton, 1, 9);

        Controls.Add(layout);
        _levelTimer.Tick += (_, _) => RefreshLiveLevel();
        RefreshState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _levelTimer.Stop();
            _sampleCapture.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RecordButtonMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _sampleCapture.IsRecording)
            return;

        var nextSamplePath = GetNextSamplePath();
        if (string.IsNullOrWhiteSpace(nextSamplePath))
        {
            _statusLabel.Text = "Collect 3 fresh samples or reset the identity before recording more.";
            return;
        }

        try
        {
            _currentSamplePath = nextSamplePath;
            _sampleCapture.Start(nextSamplePath, MicrophoneAudioSettings.From(_profile.Settings));
            _recordButton.Capture = true;
            _recordButton.Text = "Recording - release to stop";
            _recordButton.BackColor = Color.Maroon;
            _statusLabel.Text = "Recording. Keep holding the button while you say your callsign.";
            _qualityLabel.Text = "Recording in progress.";
            _levelTimer.Start();
        }
        catch (Exception ex)
        {
            _recordButton.Capture = false;
            _currentSamplePath = null;
            _statusLabel.Text = $"Microphone error: {ex.Message}";
        }
    }

    private void RecordButtonMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        StopRecording(commit: true);
    }

    private void RecordWakeButtonMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _sampleCapture.IsRecording)
            return;

        var nextSamplePath = GetNextWakeSamplePath();
        if (string.IsNullOrWhiteSpace(nextSamplePath))
        {
            _statusLabel.Text = "Collect 3 wake samples or clear wake calibration before recording more.";
            return;
        }

        try
        {
            _currentSamplePath = nextSamplePath;
            _recordingWakeSample = true;
            _sampleCapture.Start(nextSamplePath, MicrophoneAudioSettings.From(_profile.Settings));
            _recordWakeButton.Capture = true;
            _recordWakeButton.Text = "Recording Wake - release to stop";
            _recordWakeButton.BackColor = Color.DarkGreen;
            _statusLabel.Text = "Recording a wake sample. Say Callsign clearly.";
            _qualityLabel.Text = "Wake sample recording in progress.";
            _levelTimer.Start();
        }
        catch (Exception ex)
        {
            _recordWakeButton.Capture = false;
            _currentSamplePath = null;
            _recordingWakeSample = false;
            _statusLabel.Text = $"Microphone error: {ex.Message}";
        }
    }

    private void RecordWakeButtonMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        StopRecording(commit: true);
    }

    private void StopRecording(bool commit)
    {
        if (!_sampleCapture.IsRecording)
            return;

        var samplePath = _currentSamplePath ?? GetLatestSamplePath();
        try
        {
            _sampleCapture.Stop();
        }
        finally
        {
            _recordButton.Capture = false;
            _recordButton.Text = "REC Hold to Record";
            _recordButton.BackColor = Color.Firebrick;
            _recordWakeButton.Capture = false;
            _recordWakeButton.Text = "REC Wake Sample";
            _recordWakeButton.BackColor = Color.DarkOliveGreen;
            _levelTimer.Stop();
        }

        if (!commit)
        {
            _currentSamplePath = null;
            _recordingWakeSample = false;
            return;
        }

        var quality = AnalyzeSample(samplePath);
        _qualityLabel.Text = quality.Message;
        if (!quality.Accepted)
        {
            TryDelete(samplePath);
            _statusLabel.Text = "Sample rejected. Record again with a clear microphone signal.";
            _currentSamplePath = null;
            _recordingWakeSample = false;
            RefreshState();
            return;
        }

        CopySampleToLatest(samplePath);
        if (_recordingWakeSample)
            CopyWakeSampleToLatest(samplePath);
        _profile.Settings.VoiceSamplesRequired = Math.Max(3, _profile.Settings.VoiceSamplesRequired);
        _profile.Settings.VoiceSamplesRecorded = GetRecordedSampleCount();
        _profile.Settings.VoiceEnrollmentStatus = _profile.Settings.VoiceSamplesRecorded >= _profile.Settings.VoiceSamplesRequired
            ? "Ready to enroll voice identity"
            : $"Collecting sample {_profile.Settings.VoiceSamplesRecorded} of {_profile.Settings.VoiceSamplesRequired}";
        _profile.Settings.VoiceEnrolledUtc = null;
        _profileStore.Save(_profile);
        _statusLabel.Text = _recordingWakeSample
            ? "Wake sample saved. Record another wake sample or calibrate the wakeword."
            : "Sample saved. Play it back or record another sample.";
        _currentSamplePath = null;
        _recordingWakeSample = false;
        RefreshState();
    }

    private void PlayLatestSample()
    {
        var samplePath = GetLatestSamplePath();
        if (!File.Exists(samplePath))
        {
            var recordedSamples = GetRecordedSamplePaths();
            if (recordedSamples.Count == 0)
            {
                _statusLabel.Text = "Record a sample before playback.";
                return;
            }

            samplePath = recordedSamples[^1];
        }

        try
        {
            using var player = new SoundPlayer(samplePath);
            player.Play();
            _statusLabel.Text = $"Playing {Path.GetFileName(samplePath)}.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Playback failed: {ex.Message}";
        }
    }

    private void CalibrateMicrophone()
    {
        var samplePath = GetLatestSamplePath();
        if (!File.Exists(samplePath))
        {
            var recordedSamples = GetRecordedSamplePaths();
            if (recordedSamples.Count == 0)
            {
                _statusLabel.Text = "Record a sample first, then calibrate the microphone.";
                return;
            }

            samplePath = recordedSamples[^1];
        }

        var quality = AnalyzeSample(samplePath);
        if (!quality.Accepted)
        {
            _statusLabel.Text = $"Microphone calibration needs a cleaner sample: {quality.Message}";
            return;
        }

        var targetRms = Math.Max(0.01, _profile.Settings.VoiceTargetRms);
        var recommendedGain = 20.0 * Math.Log10(targetRms / Math.Max(quality.Rms, 0.001));
        recommendedGain = Math.Clamp(recommendedGain, 0, 24);
        _profile.Settings.VoiceInputGainDb = recommendedGain;
        _profile.Settings.VoiceAutoGainEnabled = true;
        _profileStore.Save(_profile);
        _qualityLabel.Text = $"Calibration used {quality.LevelState}: peak {quality.Peak:0.00}, RMS {quality.Rms:0.000}, recommended gain {recommendedGain:0.0} dB.";
        _statusLabel.Text = "Microphone calibration saved to this profile.";
        RefreshState();
    }

    private async Task CalibrateWakewordAsync()
    {
        if (_busy)
            return;

        var samplePaths = GetRecordedWakeSamplePaths().ToList();
        if (samplePaths.Count == 0)
        {
            var samplePath = GetLatestWakeSamplePath();
            if (!File.Exists(samplePath))
            {
                samplePath = GetLatestSamplePath();
                if (!File.Exists(samplePath))
                {
                    _statusLabel.Text = "Record a Callsign or wake sample first, then calibrate the wakeword.";
                    return;
                }
            }

            if (File.Exists(samplePath))
                samplePaths.Add(samplePath);
        }

        if (samplePaths.Count == 0)
        {
            _statusLabel.Text = "Record a Callsign or wake sample first, then calibrate the wakeword.";
            return;
        }

        SetBusy(true, "Scoring the wakeword samples and tuning the profile threshold...");
        try
        {
            var wakeScores = new List<(string Path, double Score)>();
            foreach (var samplePath in samplePaths)
            {
                var score = await _voiceCommandService.TryScoreWakeWordSampleAsync(samplePath, CancellationToken.None);
                if (score.HasValue)
                    wakeScores.Add((samplePath, score.Value));
            }

            if (wakeScores.Count == 0)
            {
                _statusLabel.Text = "Wake calibration could not score the sample set. Repair Wakeword or record a clearer Callsign sample.";
                return;
            }

            var bestWakeSample = wakeScores.OrderByDescending(entry => entry.Score).First();
            var calibratedThreshold = VoiceCommandService.ComputeCalibratedWakeThreshold(bestWakeSample.Score);
            if (!calibratedThreshold.HasValue)
            {
                _qualityLabel.Text = $"Wake calibration scored {wakeScores.Count} wake sample(s); best score {bestWakeSample.Score:0.000}, which is below the trusted calibration floor.";
                _statusLabel.Text = "Wake samples are too weak to calibrate yet. Record a clearer Callsign wake sample close to the mic, then calibrate again.";
                return;
            }

            VoiceCommandService.ApplyWakeCalibration(
                _profile.Settings,
                bestWakeSample.Score,
                wakeScores.Count,
                Path.GetFileName(bestWakeSample.Path));
            _profileStore.Save(_profile);
            _qualityLabel.Text = $"Wake calibration used {wakeScores.Count} wake sample(s); best score {bestWakeSample.Score:0.000}. Threshold now {_profile.Settings.VoiceWakeThreshold:0.000}.";
            _statusLabel.Text = $"Wakeword calibrated from {Path.GetFileName(bestWakeSample.Path)}.";
            RefreshState();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Wake calibration failed: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task EnrollIdentityAsync()
    {
        var samplePaths = GetRecordedSamplePaths();
        var required = Math.Max(3, _profile.Settings.VoiceSamplesRequired);
        if (samplePaths.Count < required)
        {
            _statusLabel.Text = $"Collect {required - samplePaths.Count} more fresh sample(s) before enrolling.";
            return;
        }

        SetBusy(true, "Enrolling voice identity with pyannote. This can take up to two minutes on first run...");
        try
        {
            var result = await Task.Run(() => _biometricService.EnrollFreshSamples(_profileStore, _profile, samplePaths));
            if (!result.Accepted)
            {
                _profile.Settings.VoiceEnrollmentStatus = "pyannote setup required";
                _profile.Settings.VoiceEnrolledUtc = null;
                _profileStore.Save(_profile);
                _statusLabel.Text = result.Message;
                _qualityLabel.Text = result.RejectReason == "pyannote_runtime_not_ready"
                    ? "Identity runtime is missing or still repairing."
                    : result.Message;
                RefreshState();
                return;
            }

            _profile.Settings.VoiceEnrollmentStatus = "Activated";
            _profile.Settings.VoiceEnrolledUtc = DateTime.UtcNow;
            _profile.Settings.VoiceSamplesRecorded = samplePaths.Count;
            _profileStore.Save(_profile);
            _statusLabel.Text = $"Voice identity enrolled with {result.SamplesEnrolled} fresh sample(s).";
            _qualityLabel.Text = "Enrollment completed successfully.";
            RefreshState();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Enrollment failed: {ex.Message}";
            _qualityLabel.Text = "Enrollment did not complete.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RepairIdentityRuntime()
    {
        var setupScript = Path.Combine(GetInstalledAppDirectory(), "setuppyannote.ps1");
        if (!File.Exists(setupScript))
        {
            _statusLabel.Text = $"Identity repair helper was not found: {setupScript}";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File \"{setupScript}\" -InstallPythonPackages -DownloadModel -TestEmbedding",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            });
            _statusLabel.Text = "Identity runtime repair opened. Keep the PowerShell window open until it finishes.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Unable to start identity runtime repair: {ex.Message}";
        }
    }

    private void RefreshState()
    {
        var required = Math.Max(3, _profile.Settings.VoiceSamplesRequired);
        _progress.Maximum = required;
        var recordedCount = GetRecordedSampleCount();
        if (_profile.Settings.VoiceSamplesRecorded != recordedCount)
        {
            _profile.Settings.VoiceSamplesRecorded = recordedCount;
            _profileStore.Save(_profile);
        }
        _progress.Value = Math.Min(_profile.Settings.VoiceSamplesRecorded, required);
        _progress.Style = _busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        if (_busy)
        {
            _progress.MarqueeAnimationSpeed = 20;
        }
        else
        {
            _progress.MarqueeAnimationSpeed = 0;
        }
        _sampleLabel.Text = _profile.Settings.VoiceSamplesRecorded < required
            ? $"{_profile.Settings.VoiceSamplesRecorded} / {required} samples. Voice fingerprint is weak: collect 3 fresh samples."
            : $"{_profile.Settings.VoiceSamplesRecorded} / {required} samples. Status: {_profile.Settings.VoiceEnrollmentStatus}";
        UpdateSampleRows();
        _playButton.Enabled = (File.Exists(GetLatestSamplePath()) || GetRecordedSamplePaths().Count > 0) && !_sampleCapture.IsRecording;
        _enrollButton.Enabled = _profile.Settings.VoiceSamplesRecorded >= required && !_sampleCapture.IsRecording && !_busy;
        _recordButton.Enabled = !_busy;
        _playButton.Enabled = _playButton.Enabled && !_busy;
        _repairRuntimeButton.Enabled = !_busy;
        _calibrateButton.Enabled = !_busy && File.Exists(GetLatestSamplePath());
        _wakeCalibrateButton.Enabled = !_busy && (GetRecordedWakeSamplePaths().Count > 0 || File.Exists(GetLatestWakeSamplePath()) || File.Exists(GetLatestSamplePath()) || GetRecordedSamplePaths().Count > 0);
        _wakeSampleLabel.Text = GetRecordedWakeSampleCount() == 0
            ? "No wake samples recorded yet. Use REC Wake Sample to collect Callsign-only examples."
            : $"{GetRecordedWakeSampleCount()} wake sample(s) recorded. Calibrate against these first.";
        _closeButton.Enabled = true;
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _busy = busy;
        if (!string.IsNullOrWhiteSpace(message))
            _statusLabel.Text = message;

        _progress.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
        _progress.MarqueeAnimationSpeed = busy ? 20 : 0;
        _recordButton.Enabled = !busy;
        _playButton.Enabled = !busy && (File.Exists(GetLatestSamplePath()) || GetRecordedSamplePaths().Count > 0);
        _enrollButton.Enabled = !busy && _profile.Settings.VoiceSamplesRecorded >= Math.Max(3, _profile.Settings.VoiceSamplesRequired);
        _calibrateButton.Enabled = !busy && File.Exists(GetLatestSamplePath());
        _wakeCalibrateButton.Enabled = !busy && (GetRecordedWakeSamplePaths().Count > 0 || File.Exists(GetLatestWakeSamplePath()) || File.Exists(GetLatestSamplePath()) || GetRecordedSamplePaths().Count > 0);
        _recordWakeButton.Enabled = !busy;
        _repairRuntimeButton.Enabled = !busy;
        _closeButton.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void RefreshLiveLevel()
    {
        if (!_sampleCapture.IsRecording)
            return;

        var telemetry = _sampleCapture.LastTelemetry;
        if (telemetry == null)
            return;

        _qualityLabel.Text = $"Live level: {telemetry.LevelState}. Raw RMS {telemetry.RawRms:0.000}, peak {telemetry.RawPeak:0.00}, gain {telemetry.AppliedGainDb:0.0} dB, noise floor {telemetry.NoiseFloorRms:0.000}, threshold {telemetry.SpeechThresholdRms:0.000}.";
        _statusLabel.Text = telemetry.Warnings.Count == 0
            ? "Recording. Keep holding the button while you say your callsign."
            : string.Join(" ", telemetry.Warnings);
    }

    private void UpdateSampleRows()
    {
        var samplePaths = GetRecordedSamplePaths();
        for (var index = 0; index < _sampleStatusLabels.Length; index++)
        {
            var sampleNumber = index + 1;
            var samplePath = GetSamplePath(sampleNumber);
            var label = _sampleStatusLabels[index];
            if (!File.Exists(samplePath))
            {
                label.Text = $"{Path.GetFileName(samplePath)} pending.";
                continue;
            }

            var quality = AnalyzeSample(samplePath);
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(samplePath);
            label.Text = quality.Accepted
                ? $"{Path.GetFileName(samplePath)} recorded {age.TotalMinutes:0.0} min ago. {quality.Message}"
                : $"{Path.GetFileName(samplePath)} recorded {age.TotalMinutes:0.0} min ago. {quality.Message}";
        }

        if (samplePaths.Count == 0)
            _qualityLabel.Text = "No fresh voice samples have been recorded yet.";
    }

    private int GetRecordedSampleCount() =>
        GetRecordedSamplePaths().Count;

    private int GetRecordedWakeSampleCount() =>
        GetRecordedWakeSamplePaths().Count;

    private IReadOnlyList<string> GetRecordedSamplePaths()
    {
        var folder = VoiceBiometricVerificationService.GetEnrollmentSampleFolder(_profileStore, _profile);
        if (!Directory.Exists(folder))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(folder, "sample-*.wav", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<string> GetRecordedWakeSamplePaths()
    {
        var folder = GetWakeSampleFolder();
        if (!Directory.Exists(folder))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(folder, "wake-*.wav", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string? GetNextSamplePath()
    {
        var count = GetRecordedSampleCount();
        if (count >= 3)
            return null;

        return GetSamplePath(count + 1);
    }

    private string GetSamplePath(int sampleNumber) =>
        VoiceBiometricVerificationService.GetEnrollmentSamplePath(_profileStore, _profile, sampleNumber);

    private string GetWakeSamplePath(int sampleNumber) =>
        Path.Combine(GetWakeSampleFolder(), $"wake-{sampleNumber:000}.wav");

    private void CopySampleToLatest(string samplePath)
    {
        try
        {
            var latestPath = GetLatestSamplePath();
            Directory.CreateDirectory(Path.GetDirectoryName(latestPath)!);
            if (!string.Equals(Path.GetFullPath(samplePath), Path.GetFullPath(latestPath), StringComparison.OrdinalIgnoreCase))
                File.Copy(samplePath, latestPath, overwrite: true);
        }
        catch
        {
            // Best-effort convenience copy only.
        }
    }

    private void CopyWakeSampleToLatest(string samplePath)
    {
        try
        {
            var latestPath = GetLatestWakeSamplePath();
            Directory.CreateDirectory(Path.GetDirectoryName(latestPath)!);
            if (!string.Equals(Path.GetFullPath(samplePath), Path.GetFullPath(latestPath), StringComparison.OrdinalIgnoreCase))
                File.Copy(samplePath, latestPath, overwrite: true);
        }
        catch
        {
            // Best-effort convenience copy only.
        }
    }

    private string GetLatestSamplePath()
    {
        var folder = Path.Combine(_profileStore.ResolveCallsSignFolder(_profile.Callsign), "voice-samples");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "latest.wav");
    }

    private string GetLatestWakeSamplePath()
    {
        var folder = GetWakeSampleFolder();
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "latest-wake.wav");
    }

    private string GetWakeSampleFolder()
    {
        var folder = Path.Combine(_profileStore.ResolveCallsSignFolder(_profile.Callsign), "wake-samples");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private string? GetNextWakeSamplePath()
    {
        var count = GetRecordedWakeSampleCount();
        if (count >= 3)
            return null;

        return GetWakeSamplePath(count + 1);
    }

    private static SampleQuality AnalyzeSample(string samplePath)
    {
        if (!File.Exists(samplePath))
            return new SampleQuality(false, "No sample file was captured.", 0, 0, 0, "Too quiet");

        try
        {
            using var reader = new AudioFileReader(samplePath);
            var buffer = new float[reader.WaveFormat.SampleRate * Math.Max(1, reader.WaveFormat.Channels)];
            float peak = 0;
            double sumSquares = 0;
            var sampleCount = 0;
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var index = 0; index < read; index++)
                {
                    var value = Math.Abs(buffer[index]);
                    peak = Math.Max(peak, value);
                    sumSquares += value * value;
                    sampleCount++;
                }
            }

            var duration = reader.TotalTime;
            if (duration < TimeSpan.FromMilliseconds(650))
                return new SampleQuality(false, "Sample is too short. Hold record a little longer.", peak, 0, duration.TotalSeconds, "Too quiet");
            if (sampleCount == 0 || peak < 0.015f)
                return new SampleQuality(false, "Sample is too quiet or silent.", peak, 0, duration.TotalSeconds, "Too quiet");
            if (peak > 0.98f)
                return new SampleQuality(false, "Sample is clipping. Lower microphone gain and try again.", peak, Math.Sqrt(sumSquares / Math.Max(1, sampleCount)), duration.TotalSeconds, "Clipping");

            var rms = Math.Sqrt(sumSquares / sampleCount);
            return new SampleQuality(true, $"Clean sample: {duration.TotalSeconds:0.0}s, peak {peak:0.00}, RMS {rms.ToString("0.000", CultureInfo.CurrentCulture)}.", peak, rms, duration.TotalSeconds, "Good");
        }
        catch (Exception ex)
        {
            return new SampleQuality(false, $"Sample could not be read: {ex.Message}", 0, 0, 0, "Too quiet");
        }
    }

    private static string GetInstalledAppDirectory()
    {
        var localAppDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Callsign",
            "App");
        return Directory.Exists(localAppDir) ? localAppDir : AppContext.BaseDirectory;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private sealed record SampleQuality(bool Accepted, string Message, double Peak, double Rms, double DurationSeconds, string LevelState);
}

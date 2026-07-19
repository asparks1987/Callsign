using System.Diagnostics;
using System.Drawing;
using System.Media;
using Callsign.UI.Models;
using Callsign.UI.Services;

namespace Callsign.UI;

public sealed class VoiceIdentityTrainingForm : Form
{
    private readonly ProfileStore _profileStore;
    private readonly UserProfile _profile;
    private readonly VoiceCommandService _voiceCommandService;
    private readonly VoiceSampleCaptureService _sampleCapture = new();
    private readonly VoiceBiometricVerificationService _biometricService = new();

    private readonly Label _statusLabel;
    private readonly Label _contractLabel;
    private readonly FlowLayoutPanel _statusStrip;
    private readonly Label _samplesBadge;
    private readonly Label _wakeBadge;
    private readonly Label _nextBadge;
    private readonly Label _failureBadge;
    private readonly Label _sampleLabel;
    private readonly Label _wakeSampleLabel;
    private readonly Label _wakeProvenanceLabel;
    private readonly Label _qualityLabel;
    private readonly Label _nextStepLabel;
    private readonly Label _failureLabel;
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
            RowCount = 15
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

        _contractLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 36,
            BackColor = Color.FromArgb(236, 242, 252),
            ForeColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(12, 7, 12, 7),
            Font = new Font("Segoe UI Semibold", 8.8f, FontStyle.Bold),
            Text = "Contract: record sample -> review voice state -> enroll voice identity.",
            AccessibleName = "Voice identity training contract",
            AccessibleDescription = "Summarizes the visible voice-identity flow from sample capture through review and enrollment."
        };
        layout.Controls.Add(_contractLabel, 0, 1);
        layout.SetColumnSpan(_contractLabel, 2);

        _statusStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 4),
            Padding = Padding.Empty,
            AccessibleName = "Voice identity training status strip",
            AccessibleDescription = "Shows the current sample count, wake status, next step, and failure state as compact visible badges."
        };
        _samplesBadge = CreateStatusBadge("Samples: 0", "Shows how many fresh samples have been recorded.", Color.FromArgb(239, 246, 255), Color.FromArgb(30, 64, 175));
        _wakeBadge = CreateStatusBadge("Wake: waiting", "Shows the wake calibration state.", Color.FromArgb(243, 244, 246), Color.FromArgb(51, 65, 85));
        _nextBadge = CreateStatusBadge("Next: record 3 fresh samples", "Shows the next visible training step.", Color.FromArgb(250, 245, 255), Color.FromArgb(109, 40, 217));
        _failureBadge = CreateStatusBadge("Failure: none", "Shows the current training failure state.", Color.FromArgb(236, 253, 245), Color.FromArgb(6, 95, 70));
        _statusStrip.Controls.Add(_samplesBadge);
        _statusStrip.Controls.Add(_wakeBadge);
        _statusStrip.Controls.Add(_nextBadge);
        _statusStrip.Controls.Add(_failureBadge);
        layout.Controls.Add(_statusStrip, 0, 2);
        layout.SetColumnSpan(_statusStrip, 2);

        var prompt = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Multiline = true,
            Height = 72,
            Text = $"Say: Callsign {profile.Callsign}. Hold the record button while speaking, release to save the sample, then enroll the voice identity."
        };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Prompt" }, 0, 3);
        layout.Controls.Add(prompt, 1, 3);

        _progress = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = Math.Max(3, profile.Settings.VoiceSamplesRequired) };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Samples" }, 0, 4);
        layout.Controls.Add(_progress, 1, 4);

        _sampleLabel = new Label { AutoSize = true };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Progress" }, 0, 5);
        layout.Controls.Add(_sampleLabel, 1, 5);

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
        layout.Controls.Add(new Label { AutoSize = true, Text = "Voice samples" }, 0, 6);
        layout.Controls.Add(sampleRows, 1, 6);

        _wakeSampleLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "No wake samples recorded yet." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Wake samples" }, 0, 7);
        layout.Controls.Add(_wakeSampleLabel, 1, 7);

        _wakeProvenanceLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "Wake provenance: pending trusted sample set." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Wake provenance" }, 0, 8);
        layout.Controls.Add(_wakeProvenanceLabel, 1, 8);

        _qualityLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "No sample analyzed yet." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Audio quality" }, 0, 9);
        layout.Controls.Add(_qualityLabel, 1, 9);

        _nextStepLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "Next step: record 3 fresh samples before enrollment." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Next step" }, 0, 10);
        layout.Controls.Add(_nextStepLabel, 1, 10);

        _failureLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "Failure type: none yet." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Failure type" }, 0, 11);
        layout.Controls.Add(_failureLabel, 1, 11);

        _statusLabel = new Label { AutoSize = true, MaximumSize = new Size(540, 0), Text = "Ready to record." };
        layout.Controls.Add(new Label { AutoSize = true, Text = "Status" }, 0, 12);
        layout.Controls.Add(_statusLabel, 1, 12);

        _recordButton = new Button
        {
            Text = "REC Hold to Record",
            Width = 170,
            Height = 44,
            BackColor = Color.Firebrick,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Font, FontStyle.Bold),
            Cursor = Cursors.Hand,
            AccessibleName = "Voice identity record sample",
            AccessibleDescription = "Voice phrase: record voice identity sample."
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
            Cursor = Cursors.Hand,
            AccessibleName = "Voice identity record wake sample",
            AccessibleDescription = "Voice phrase: record wake sample."
        };
        _recordWakeButton.FlatAppearance.BorderSize = 0;
        _recordWakeButton.MouseDown += RecordWakeButtonMouseDown;
        _recordWakeButton.MouseUp += RecordWakeButtonMouseUp;
        _recordWakeButton.MouseLeave += (_, _) =>
        {
            if (_sampleCapture.IsRecording)
                _recordWakeButton.Capture = true;
        };

        _playButton = new Button { Text = "Play Sample", Width = 120, Height = 44, AccessibleName = "Voice identity play sample", AccessibleDescription = "Voice phrase: play voice identity sample." };
        _playButton.Click += (_, _) => PlayLatestSample();

        _enrollButton = new Button { Text = "Enroll Voice Identity", Width = 170, Height = 44, AccessibleName = "Voice identity enroll", AccessibleDescription = "Voice phrase: enroll voice identity." };
        _enrollButton.Click += async (_, _) => await EnrollIdentityAsync();

        _calibrateButton = new Button { Text = "Calibrate Mic", Width = 130, Height = 44, AccessibleName = "Voice identity calibrate microphone", AccessibleDescription = "Voice phrase: calibrate microphone." };
        _calibrateButton.Click += (_, _) => CalibrateMicrophone();

        _wakeCalibrateButton = new Button { Text = "Calibrate Wakeword", Width = 180, Height = 44, AccessibleName = "Voice identity calibrate wakeword", AccessibleDescription = "Voice phrase: calibrate wakeword." };
        _wakeCalibrateButton.Click += async (_, _) => await CalibrateWakewordAsync();

        _repairRuntimeButton = new Button { Text = "Repair Identity Runtime", Width = 180, Height = 44, AccessibleName = "Voice identity repair runtime", AccessibleDescription = "Voice phrase: repair identity runtime." };
        _repairRuntimeButton.Click += (_, _) => RepairIdentityRuntime();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        buttons.Controls.Add(_recordButton);
        buttons.Controls.Add(_recordWakeButton);
        buttons.Controls.Add(_playButton);
        buttons.Controls.Add(_enrollButton);
        buttons.Controls.Add(_calibrateButton);
        buttons.Controls.Add(_wakeCalibrateButton);
        buttons.Controls.Add(_repairRuntimeButton);
        layout.Controls.Add(buttons, 1, 13);

        _closeButton = new Button { Text = "Close", Width = 100, AccessibleName = "Voice identity close", AccessibleDescription = "Voice phrase: close voice identity training." };
        _closeButton.Click += (_, _) => Close();
        CancelButton = _closeButton;
        layout.Controls.Add(_closeButton, 1, 14);

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

    public string ContractText => _contractLabel.Text;
    public string ContractAccessibleName => _contractLabel.AccessibleName ?? string.Empty;
    public string ContractAccessibleDescription => _contractLabel.AccessibleDescription ?? string.Empty;
    public string StatusStripAccessibleName => _statusStrip.AccessibleName ?? string.Empty;
    public string SamplesBadgeText => _samplesBadge.Text;
    public string WakeBadgeText => _wakeBadge.Text;
    public string NextBadgeText => _nextBadge.Text;
    public string FailureBadgeText => _failureBadge.Text;

    private void RecordButtonMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _sampleCapture.IsRecording)
            return;

        var nextSamplePath = GetNextSamplePath();
        if (string.IsNullOrWhiteSpace(nextSamplePath))
        {
            _statusLabel.Text = "Collect 3 fresh samples or reset the identity before recording more.";
            _failureLabel.Text = "Failure type: sample quota reached.";
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
            _failureLabel.Text = $"Failure type: microphone. {GetFailureHint(ex.Message)}";
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
            _failureLabel.Text = "Failure type: wake sample quota reached.";
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
            _failureLabel.Text = $"Failure type: microphone. {GetFailureHint(ex.Message)}";
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
            _failureLabel.Text = "Failure type: microphone.";
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
        _failureLabel.Text = "Failure type: none.";
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
                _failureLabel.Text = "Failure type: microphone.";
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
        _failureLabel.Text = "Failure type: none.";
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
                _failureLabel.Text = "Failure type: wake runtime or model.";
                return;
            }

            var bestWakeSample = wakeScores.OrderByDescending(entry => entry.Score).First();
            var trustedWakeScore = VoiceCommandService.ComputeTrustedWakeCalibrationScore(wakeScores.Select(entry => entry.Score).ToArray());
            var calibratedThreshold = VoiceCommandService.ComputeCalibratedWakeThreshold(wakeScores.Select(entry => entry.Score).ToArray());
            if (!calibratedThreshold.HasValue)
            {
                _qualityLabel.Text = $"Wake calibration scored {wakeScores.Count} wake sample(s); trusted score {trustedWakeScore:0.000}, best score {bestWakeSample.Score:0.000}, which is below the trusted calibration floor.";
                _statusLabel.Text = "Wake samples are too weak to calibrate yet. Record a clearer Callsign wake sample close to the mic, then calibrate again.";
                _failureLabel.Text = "Failure type: wake model confidence.";
                return;
            }

            VoiceCommandService.ApplyWakeCalibration(
                _profile.Settings,
                wakeScores.Select(entry => entry.Score).ToArray(),
                Path.GetFileName(bestWakeSample.Path));
            _profileStore.Save(_profile);
            _qualityLabel.Text = $"Wake calibration used {wakeScores.Count} wake sample(s); trusted score {trustedWakeScore:0.000}, best score {bestWakeSample.Score:0.000}. Threshold now {_profile.Settings.VoiceWakeThreshold:0.000}.";
            _statusLabel.Text = $"Wakeword calibrated from {Path.GetFileName(bestWakeSample.Path)} using the trusted sample set.";
            _failureLabel.Text = "Failure type: none.";
            RefreshState();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Wake calibration failed: {ex.Message}";
            _failureLabel.Text = $"Failure type: service. {GetFailureHint(ex.Message)}";
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
            _failureLabel.Text = "Failure type: not enough samples.";
            return;
        }

        SetBusy(true, "Enrolling voice identity with pyannote. This can take up to two minutes on first run...");
        try
        {
            var result = await Task.Run(() => _biometricService.EnrollFreshSamples(_profileStore, _profile, samplePaths));
            if (!result.Accepted)
            {
                _profile.Settings.VoiceEnrollmentStatus = VoiceBiometricVerificationService.IsSampleProofRejectReason(result.RejectReason)
                    ? result.Message
                    : "pyannote setup required";
                _profile.Settings.VoiceEnrolledUtc = null;
                _profileStore.Save(_profile);
                _statusLabel.Text = result.Message;
                _qualityLabel.Text = result.RejectReason == "pyannote_runtime_not_ready"
                    ? "Identity runtime is missing or still repairing."
                    : result.Message;
                _failureLabel.Text = DescribeEnrollmentFailure(result.RejectReason, result.Message);
                RefreshState();
                return;
            }

            _profile.Settings.VoiceEnrollmentStatus = "Activated";
            _profile.Settings.VoiceEnrolledUtc = DateTime.UtcNow;
            _profile.Settings.VoiceSamplesRecorded = samplePaths.Count;
            _profileStore.Save(_profile);
            _statusLabel.Text = $"Voice identity enrolled with {result.SamplesEnrolled} fresh sample(s).";
            _qualityLabel.Text = "Enrollment completed successfully.";
            _failureLabel.Text = "Failure type: none.";
            RefreshState();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Enrollment failed: {ex.Message}";
            _qualityLabel.Text = "Enrollment did not complete.";
            _failureLabel.Text = $"Failure type: service. {GetFailureHint(ex.Message)}";
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
        _nextStepLabel.Text = BuildNextStepText(required);
        _failureLabel.Text = BuildFailureText(required);
        _samplesBadge.Text = $"Samples: {_profile.Settings.VoiceSamplesRecorded} / {required}";
        var wakeThreshold = _profile.Settings.VoiceWakeThreshold;
        _wakeBadge.Text = wakeThreshold > 0
            ? $"Wake: threshold {wakeThreshold:0.000}"
            : "Wake: waiting";
        var wakeSource = string.IsNullOrWhiteSpace(_profile.Settings.VoiceWakeCalibrationSource)
            ? null
            : _profile.Settings.VoiceWakeCalibrationSource.Trim();
        _wakeProvenanceLabel.Text = _profile.Settings.VoiceWakeCalibrationSampleCount > 0
            ? string.IsNullOrWhiteSpace(wakeSource)
                ? $"Wake provenance: trusted sample set ({_profile.Settings.VoiceWakeCalibrationSampleCount} sample(s))."
                : $"Wake provenance: trusted sample set ({_profile.Settings.VoiceWakeCalibrationSampleCount} sample(s), source {wakeSource})."
            : "Wake provenance: pending trusted sample set.";
        _nextBadge.Text = _nextStepLabel.Text.StartsWith("Next step:", StringComparison.OrdinalIgnoreCase)
            ? "Next: " + _nextStepLabel.Text["Next step: ".Length..]
            : "Next: record fresh samples";
        _failureBadge.Text = _failureLabel.Text.StartsWith("Failure type:", StringComparison.OrdinalIgnoreCase)
            ? _failureLabel.Text.Replace("Failure type:", "Failure:", StringComparison.OrdinalIgnoreCase)
            : "Failure: none";
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

    public string NextStepText => _nextStepLabel.Text;
    public string FailureText => _failureLabel.Text;
    public string WakeProvenanceText => _wakeProvenanceLabel.Text;

    private static Label CreateStatusBadge(string text, string description, Color backColor, Color foreColor)
    {
        return new Label
        {
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 0, 8, 6),
            BackColor = backColor,
            ForeColor = foreColor,
            Text = text,
            Font = new Font("Segoe UI", 8.4f, FontStyle.Bold),
            AccessibleName = text,
            AccessibleDescription = description
        };
    }

    private string BuildNextStepText(int required)
    {
        if (_busy)
            return "Next step: wait for enrollment or calibration to finish.";

        var recordedCount = _profile.Settings.VoiceSamplesRecorded;
        if (recordedCount < required)
            return $"Next step: record {required - recordedCount} more fresh sample(s).";

        if (_profile.Settings.VoiceEnrolledUtc.HasValue)
            return "Next step: voice identity is enrolled and ready.";

        return "Next step: enroll voice identity.";
    }

    private string BuildFailureText(int required)
    {
        if (_busy)
            return "Failure type: service. Next: wait for enrollment or calibration to finish.";

        if (_profile.Settings.VoiceSamplesRecorded < required)
            return "Failure type: not enough samples yet. Next: record more fresh samples.";

        if (_profile.Settings.VoiceEnrolledUtc.HasValue)
            return "Failure type: none.";

        var proof = VoiceBiometricVerificationService.ReadEnrollmentSampleProof(_profileStore, _profile);
        if (proof is { Accepted: false } && proof.SampleCount >= required && VoiceBiometricVerificationService.IsSampleProofRejectReason(proof.RejectReason))
            return VoiceBiometricVerificationService.DescribeEnrollmentFailureType(proof.RejectReason, proof.Message, proof);

        if (string.Equals(_profile.Settings.VoiceEnrollmentStatus, "pyannote setup required", StringComparison.OrdinalIgnoreCase))
            return "Failure type: identity runtime or model cache. Next: choose Repair Identity Runtime.";

        if ((_profile.Settings.VoiceEnrollmentStatus ?? string.Empty).Contains("collecting sample", StringComparison.OrdinalIgnoreCase))
            return "Failure type: sample collection in progress. Next: keep recording until the sample is saved.";

        return "Failure type: identity runtime, model cache, or service. Next: choose Repair Identity Runtime.";
    }

    private static string DescribeEnrollmentFailure(string? rejectReason, string message)
    {
        return VoiceBiometricVerificationService.DescribeEnrollmentFailureType(rejectReason, message);
    }

    private static string GetFailureHint(string message)
    {
        if (message.Contains("microphone", StringComparison.OrdinalIgnoreCase))
            return "Check microphone permissions and device selection.";

        if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return "Identity runtime is starting. Try again in a moment.";

        if (message.Contains("runtime", StringComparison.OrdinalIgnoreCase))
            return "Use Repair Identity Runtime or Repair Wakeword if prompted.";

        return "Check the status text and retry.";
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
        var quality = VoiceSampleQualityAnalyzer.Analyze(samplePath);
        return new SampleQuality(
            quality.Accepted,
            quality.Message,
            quality.Peak,
            quality.Rms,
            quality.DurationSeconds,
            quality.State);
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

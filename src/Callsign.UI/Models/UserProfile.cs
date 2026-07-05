using System.Text.Json.Serialization;

namespace Callsign.UI.Models;

public sealed class UserSettings
{
    public string WakeWord { get; set; } = "Callsign";
    public string PreferredTheme { get; set; } = "Dark";
    public string LanguageCode { get; set; } = "en-US";
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    public bool StartWithWindows { get; set; } = false;
    public bool ShowCommandFeed { get; set; } = true;
    public string DashboardTitle { get; set; } = "Callsign";
    public string VoiceEnrollmentStatus { get; set; } = "Not activated";
    public int VoiceSamplesRecorded { get; set; } = 0;
    public int VoiceSamplesRequired { get; set; } = 3;
    public DateTime? VoiceEnrolledUtc { get; set; }
    public string VoiceRecognitionMode { get; set; } = "Local";
    public List<string> VoiceCallsignAliases { get; set; } = [];
    public double VoiceInputGainDb { get; set; } = 12;
    public bool VoiceAutoGainEnabled { get; set; } = true;
    public double VoiceTargetRms { get; set; } = 0.08;
    public bool VoiceAdaptiveSpeechThresholdEnabled { get; set; } = true;
    public bool VoiceBiometricRequired { get; set; } = true;
    public double VoiceBiometricThreshold { get; set; } = 0.72;
    public double VoiceBiometricNearMatchThreshold { get; set; } = 0.86;
    public int VoiceBiometricMaxCandidateAgeSeconds { get; set; } = 30;
    public string? VoiceModelPath { get; set; }
    public double VoiceWakeThreshold { get; set; } = 0;
    public string VoiceWakeSensitivity { get; set; } = "More responsive";
    public string? VoiceWakeCalibrationVersion { get; set; }
    public int VoiceWakeCalibrationSampleCount { get; set; }
    public DateTime? VoiceWakeCalibratedUtc { get; set; }
    public string? VoiceWakeCalibrationSource { get; set; }
    public bool VoiceWakeDiagnosticsEnabled { get; set; }
    public int VoiceSilenceMilliseconds { get; set; } = 200;
    public double VoiceCommandConfidenceThreshold { get; set; } = 0.65;
    public bool VoiceCloudOptIn { get; set; }
    public bool UseVoiceActivityDetection { get; set; } = true;
    public bool UseNoiseSuppression { get; set; }
    public string? LastLaunchedApp { get; set; }
    public List<string> DictationVocabulary { get; set; } = [];
    public bool DictationFluidModeEnabled { get; set; }
    public bool DictationAutomaticPunctuationEnabled { get; set; } = true;
    public bool DictationProfanityFilterEnabled { get; set; } = true;
}

public sealed class UserProfile
{
    [JsonIgnore]
    public string Callsign { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public UserSettings Settings { get; set; } = new();

    [JsonIgnore]
    public string StorageLabel => string.IsNullOrWhiteSpace(DisplayName) ? Callsign : $"{DisplayName} ({Callsign})";

    public string NormalizeForStorage()
    {
        return Callsign.Trim().ToLowerInvariant();
    }
}

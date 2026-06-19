using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace Callsign.Setup;

internal sealed class InstallerForm : Form
{
    private readonly InstallerWorkflow _workflow;
    private InstallerDiscovery _discovery;
    private readonly List<string> _readoutLines = [];
    private CancellationTokenSource _cancelSource = new();

    private Label _bannerLabel = null!;
    private Label _detailsLabel = null!;
    private Label _actionLabel = null!;
    private ProgressBar _progressBar = null!;
    private Label _stageLabel = null!;
    private ListBox _readoutList = null!;
    private Button _installButton = null!;
    private Button _repairButton = null!;
    private Button _uninstallButton = null!;
    private Button _cancelButton = null!;
    private Button _refreshButton = null!;
    private CheckBox _launchAfterInstallCheckBox = null!;
    private bool _busy;

    public InstallerForm(InstallerWorkflow workflow)
    {
        _workflow = workflow;
        _discovery = workflow.Detect();

        Text = "Callsign Installer";
        Width = 860;
        Height = 620;
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        BuildUi();
        UpdateDiscoveryUi();
        Shown += (_, _) => FocusDefaultAction();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _bannerLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            Text = "Callsign Setup"
        };

        _detailsLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(760, 0)
        };

        _actionLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            ForeColor = Color.DimGray
        };

        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        _installButton = new Button { Width = 140, Height = 40, Text = "Install" };
        _installButton.Click += async (_, _) => await RunActionAsync(InstallerAction.Install);

        _repairButton = new Button { Width = 140, Height = 40, Text = "Repair" };
        _repairButton.Click += async (_, _) => await RunActionAsync(InstallerAction.Repair);

        _uninstallButton = new Button { Width = 140, Height = 40, Text = "Uninstall" };
        _uninstallButton.Click += async (_, _) => await RunActionAsync(InstallerAction.Uninstall);

        _refreshButton = new Button { Width = 120, Height = 40, Text = "Refresh" };
        _refreshButton.Click += (_, _) =>
        {
            _discovery = _workflow.Detect();
            UpdateDiscoveryUi();
            FocusDefaultAction();
        };

        _cancelButton = new Button { Width = 120, Height = 40, Text = "Cancel", Enabled = false };
        _cancelButton.Click += (_, _) => _cancelSource.Cancel();

        _launchAfterInstallCheckBox = new CheckBox
        {
            AutoSize = true,
            Checked = true,
            Text = "Launch Callsign after install or repair",
            Padding = new Padding(6, 8, 0, 0)
        };

        actionsPanel.Controls.Add(_installButton);
        actionsPanel.Controls.Add(_repairButton);
        actionsPanel.Controls.Add(_uninstallButton);
        actionsPanel.Controls.Add(_refreshButton);
        actionsPanel.Controls.Add(_cancelButton);

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 24,
            Minimum = 0,
            Maximum = 100
        };

        _stageLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Ready."
        };

        _readoutList = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true
        };

        var readoutPanel = new GroupBox
        {
            Dock = DockStyle.Fill,
            Text = "File readout / progress log"
        };
        readoutPanel.Controls.Add(_readoutList);
        _readoutList.BringToFront();

        root.Controls.Add(_bannerLabel, 0, 0);
        var topPanel = new Panel { Dock = DockStyle.Top, AutoSize = true };
        topPanel.Controls.Add(_actionLabel);
        topPanel.Controls.Add(_detailsLabel);
        root.Controls.Add(topPanel, 0, 1);
        root.Controls.Add(readoutPanel, 0, 2);

        var bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 4
        };
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomPanel.Controls.Add(_progressBar, 0, 0);
        bottomPanel.Controls.Add(_stageLabel, 0, 1);
        bottomPanel.Controls.Add(_launchAfterInstallCheckBox, 0, 2);
        bottomPanel.Controls.Add(actionsPanel, 0, 3);
        root.Controls.Add(bottomPanel, 0, 3);

        Controls.Add(root);
    }

    private void UpdateDiscoveryUi()
    {
        var summary = _discovery.HasPreviousInstall
            ? "A previous Callsign install was detected."
            : "No previous Callsign install was detected.";

        _detailsLabel.Text = $"{summary}\r\nInstall folder: {_discovery.InstallDirectory}\r\nModel folder: {_discovery.ModelDirectory}\r\nLogs folder: {_discovery.LogsDirectory}";
        _actionLabel.Text = _discovery.HasPreviousInstall
            ? "Choose Install to reinstall over the existing copy, Repair to fix missing or damaged files, or Uninstall to remove Callsign."
            : "Choose Install to set up Callsign, or Repair if you already have a partial install on this device.";

        _installButton.Enabled = !_busy;
        _repairButton.Enabled = !_busy && _discovery.HasPreviousInstall;
        _uninstallButton.Enabled = !_busy && _discovery.HasPreviousInstall;
        _refreshButton.Enabled = !_busy;
        _cancelButton.Enabled = _busy;
        _launchAfterInstallCheckBox.Enabled = !_busy;
    }

    private void FocusDefaultAction()
    {
        if (_discovery.HasPreviousInstall && _repairButton.Enabled)
        {
            AcceptButton = _repairButton;
            _repairButton.Focus();
            return;
        }

        AcceptButton = _installButton;
        _installButton.Focus();
    }

    private async Task RunActionAsync(InstallerAction action)
    {
        if (_busy)
            return;

        var confirmation = action switch
        {
            InstallerAction.Install when _discovery.HasPreviousInstall =>
                MessageBox.Show(this, "A previous Callsign install was found. Install will reinstall over the existing copy. Continue?", "Confirm Install", MessageBoxButtons.OKCancel, MessageBoxIcon.Question),
            InstallerAction.Install =>
                MessageBox.Show(this, "Install Callsign on this device?", "Confirm Install", MessageBoxButtons.OKCancel, MessageBoxIcon.Question),
            InstallerAction.Repair =>
                MessageBox.Show(this, "Repair the existing Callsign install?", "Confirm Repair", MessageBoxButtons.OKCancel, MessageBoxIcon.Question),
            InstallerAction.Uninstall =>
                MessageBox.Show(this, "Uninstall Callsign and remove its local install folder, shortcuts, runtime files, and service registration?", "Confirm Uninstall", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning),
            _ => DialogResult.Cancel
        };

        if (confirmation != DialogResult.OK)
            return;

        _busy = true;
        _cancelSource.Cancel();
        _cancelSource.Dispose();
        _cancelSource = new CancellationTokenSource();

        _readoutLines.Clear();
        _readoutList.Items.Clear();
        _progressBar.Value = 0;
        _stageLabel.Text = "Starting...";
        UpdateDiscoveryUi();
        AppendReadout($"Starting {action.ToString().ToLowerInvariant()}...");

        try
        {
            var progress = new Progress<InstallerProgressEvent>(eventData =>
            {
                _progressBar.Value = Math.Clamp(eventData.Percent, _progressBar.Minimum, _progressBar.Maximum);
                _stageLabel.Text = $"{eventData.Stage}: {eventData.Message}";
                AppendReadout(FormatReadout(eventData));
            });

            await _workflow.ExecuteAsync(action, progress, _cancelSource.Token);
            _discovery = _workflow.Detect();
            AppendReadout($"{action} completed.");
            MessageBox.Show(this, $"{action} completed successfully.", "Callsign Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (_launchAfterInstallCheckBox.Checked && action is InstallerAction.Install or InstallerAction.Repair)
            {
                LaunchInstalledCallsign();
            }
        }
        catch (OperationCanceledException)
        {
            AppendReadout("Operation cancelled.");
            MessageBox.Show(this, "The operation was cancelled.", "Callsign Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppendReadout($"ERROR: {ex.Message}");
            MessageBox.Show(this, $"Callsign setup failed.\r\n\r\n{ex.Message}", "Callsign Installer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            UpdateDiscoveryUi();
            FocusDefaultAction();
        }
    }

    private void AppendReadout(string text)
    {
        _readoutLines.Add(text);
        _readoutList.Items.Add(text);
        _readoutList.TopIndex = Math.Max(0, _readoutList.Items.Count - 1);
    }

    private void LaunchInstalledCallsign()
    {
        var installedUi = Path.Combine(_discovery.InstallDirectory, "Callsign.UI.exe");
        if (!File.Exists(installedUi))
        {
            AppendReadout($"Installed Callsign UI was not found at {installedUi}.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installedUi,
                WorkingDirectory = _discovery.InstallDirectory,
                UseShellExecute = true
            });
            AppendReadout("Launched Callsign UI for testing.");
        }
        catch (Exception ex)
        {
            AppendReadout($"Unable to launch Callsign UI: {ex.Message}");
            MessageBox.Show(this, $"Callsign was installed, but the UI could not be launched.\r\n\r\n{ex.Message}", "Callsign Installer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string FormatReadout(InstallerProgressEvent progress) =>
        string.IsNullOrWhiteSpace(progress.FilePath)
            ? $"{progress.Percent,3}% {progress.Message}"
            : $"{progress.Percent,3}% {progress.Message} - {progress.FilePath}";
}

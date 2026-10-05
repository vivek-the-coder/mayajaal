using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MayaJaal.SecurityCenter.Services;
using MayaJaal.Shared.Models;
using Microsoft.Win32;

namespace MayaJaal.SecurityCenter.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly GuardianStatusService _guardian;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _clockTimer;
    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private bool _refreshing;
    private bool _disposed;

    public MainViewModel(GuardianStatusService guardian)
    {
        _guardian = guardian;
        Events = new ObservableCollection<EventRow>();
        Incidents = new ObservableCollection<IncidentRow>();
        Kpis = new ObservableCollection<KpiCard>();
        AlertBars = new ObservableCollection<AlertBar>();
        MonthBars = new ObservableCollection<MonthBar>();
        CategoryTrends = new ObservableCollection<CategoryTrend>();
        VaultRows = new ObservableCollection<VaultRow>();
        VaultItemRows = new ObservableCollection<VaultItemRow>();
        PolicyRows = new ObservableCollection<PolicyRow>();
        AttackSteps = new ObservableCollection<AttackStepRow>();
        SeedAttackSteps("usb-exfil");

        SeedEmptyDashboard();
        UpdateGettingStarted(offline: true, hasVaults: false, hasEvents: false, hasIncident: false);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await RefreshAsync();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => ClockText = DateTime.Now.ToString("HH:mm:ss");
        ClockText = DateTime.Now.ToString("HH:mm:ss");
    }

    [ObservableProperty] private string _selectedNav = "Dashboard";
    [ObservableProperty] private bool _isDashboardVisible = true;
    [ObservableProperty] private bool _isIncidentsVisible;
    [ObservableProperty] private bool _isVaultVisible;
    [ObservableProperty] private bool _isPoliciesVisible;
    [ObservableProperty] private bool _isDetectionsVisible;
    [ObservableProperty] private bool _isAttackLabVisible;
    [ObservableProperty] private bool _isSearchVisible;
    [ObservableProperty] private bool _isSettingsVisible;
    [ObservableProperty] private bool _isHelpVisible;
    [ObservableProperty] private bool _isPreferencesVisible;
    [ObservableProperty] private bool _isSimulationRunning;
    [ObservableProperty] private string _simulationStatus = "Ready — choose a scenario and press Run.";
    [ObservableProperty] private string _simulationResult = "";
    [ObservableProperty] private string _selectedScenario = "usb-exfil";
    [ObservableProperty] private string _scenarioTitle = "USB data exfiltration";
    [ObservableProperty] private string _scenarioNarrative =
        "An attacker plugs in a USB drive, launches a suspicious exfil tool, touches a decoy (honey) file, then bulk-copies documents to removable media.";
    [ObservableProperty] private string _defenseNarrative =
        "MayaJaal correlates USB + process + honey + mass-copy signals, raises risk/confidence, opens an incident, and can execute reversible vault lockdown.";

    [ObservableProperty] private string _threatLevelText = "UNKNOWN";
    [ObservableProperty] private string _threatLevelBrush = "#71717A";
    [ObservableProperty] private string _threatSubText = "Waiting for Guardian";
    [ObservableProperty] private string _riskScoreText = "--";
    [ObservableProperty] private double _riskBarValue;
    [ObservableProperty] private string _riskLevelText = "—";
    [ObservableProperty] private string _riskLevelBrush = "#71717A";
    [ObservableProperty] private string _confidenceText = "--";
    [ObservableProperty] private double _confidenceBarValue;
    [ObservableProperty] private string _activeIncidentsText = "0";
    [ObservableProperty] private string _incidentSummaryText = "No active incidents";
    [ObservableProperty] private string _eventCountText = "0";
    [ObservableProperty] private string _connectionStatus = "OFFLINE";
    [ObservableProperty] private string _connectionBrush = "#EF4444";
    [ObservableProperty] private string _statusBarText = "Waiting for Guardian IPC...";
    [ObservableProperty] private string _healthText = "Connecting…";
    [ObservableProperty] private string _healthBrush = "#71717A";
    [ObservableProperty] private string _lastUpdatedText = "--";
    [ObservableProperty] private string _clockText = "--:--:--";
    [ObservableProperty] private bool _isOffline = true;
    [ObservableProperty] private bool _hasActiveIncident;
    [ObservableProperty] private string _incidentPanelBrush = "#F4F4F5";
    [ObservableProperty] private string _vaultStatus = "Ready";
    [ObservableProperty] private bool _hasIncidents;
    [ObservableProperty] private bool _hasVaults;
    [ObservableProperty] private bool _hasVaultItems;
    [ObservableProperty] private bool _hasPolicies;
    [ObservableProperty] private bool _isVaultUnlocked;
    [ObservableProperty] private string _activeVaultId = "";
    [ObservableProperty] private string _pipeName = "MayaJaal.Guardian";
    [ObservableProperty] private string _settingsSummary = "Waiting for Guardian…";
    [ObservableProperty] private string _gettingStartedText =
        "1) Start Guardian  ·  2) Unlock vault  ·  3) Add a file  ·  4) Try Attack Lab";
    [ObservableProperty] private bool _showGettingStarted = true;
    [ObservableProperty] private string _nextActionText = "Waiting for Guardian…";

    [ObservableProperty] private string _incidentNumber = "—";
    [ObservableProperty] private string _incidentSignal = "—";
    [ObservableProperty] private string _incidentStatus = "—";
    [ObservableProperty] private string _incidentStatusBrush = "#71717A";
    [ObservableProperty] private string _incidentAction = "—";
    [ObservableProperty] private string _incidentActionBrush = "#18181B";
    [ObservableProperty] private string _incidentRisk = "—";
    [ObservableProperty] private string _incidentStarted = "—";
    [ObservableProperty] private string _incidentTimelineText = "No timeline yet.";

    [ObservableProperty] private string _breadcrumbTrail = "Dashboard / Overview";
    [ObservableProperty] private string _userDisplayName = Environment.UserName;
    [ObservableProperty] private string _timeRangeLabel = "Last 24 hours";

    [ObservableProperty] private string _healthIssuesPctText = "0%";
    [ObservableProperty] private string _healthGoodPctText = "100%";
    [ObservableProperty] private string _healthSnoozedPctText = "0%";
    [ObservableProperty] private string _donutIssuesPath = "";
    [ObservableProperty] private string _donutGoodPath = "";
    [ObservableProperty] private string _donutSnoozedPath = "";
    [ObservableProperty] private string _donutHolePath = "";
    [ObservableProperty] private string _donutCenterLabel = "100%";
    [ObservableProperty] private string _donutCenterSub = "Healthy";

    public ObservableCollection<EventRow> Events { get; }
    public ObservableCollection<IncidentRow> Incidents { get; }
    public ObservableCollection<KpiCard> Kpis { get; }
    public ObservableCollection<AlertBar> AlertBars { get; }
    public ObservableCollection<MonthBar> MonthBars { get; }
    public ObservableCollection<CategoryTrend> CategoryTrends { get; }
    public ObservableCollection<VaultRow> VaultRows { get; }
    public ObservableCollection<VaultItemRow> VaultItemRows { get; }
    public ObservableCollection<PolicyRow> PolicyRows { get; }
    public ObservableCollection<AttackStepRow> AttackSteps { get; }

    public string SystemInfo => $"Windows {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}";
    public string UptimeText => FormatUptime(DateTime.UtcNow - _startedUtc);

    public string DashboardNavTag => NavTag("Dashboard");
    public string IncidentsNavTag => NavTag("Incidents");
    public string VaultNavTag => NavTag("Vault");
    public string PoliciesNavTag => NavTag("Policies");
    public string DetectionsNavTag => NavTag("Detections");
    public string AttackLabNavTag => NavTag("AttackLab");
    public string SettingsNavTag => NavTag("Settings");

    private string NavTag(string page) => SelectedNav == page ? "Active" : "Idle";

    public void Start()
    {
        _timer.Start();
        _clockTimer.Start();
        _ = RefreshAsync();
    }

    partial void OnSelectedNavChanged(string value)
    {
        OnPropertyChanged(nameof(DashboardNavTag));
        OnPropertyChanged(nameof(IncidentsNavTag));
        OnPropertyChanged(nameof(VaultNavTag));
        OnPropertyChanged(nameof(PoliciesNavTag));
        OnPropertyChanged(nameof(DetectionsNavTag));
        OnPropertyChanged(nameof(AttackLabNavTag));
        OnPropertyChanged(nameof(SettingsNavTag));
        BreadcrumbTrail = value switch
        {
            "Dashboard" => "Dashboard / Overview",
            "Incidents" => "Incidents / Cases",
            "Vault" => "Vault / Containment",
            "Policies" => "Policies / Response rules",
            "Detections" => "Detections / Categories",
            "AttackLab" => "Attack Lab / Faculty demo",
            "Settings" => "Settings",
            _ => "Dashboard / Overview"
        };
    }

    [RelayCommand]
    private void Navigate(string? page)
    {
        SelectedNav = page ?? "Dashboard";
        IsDashboardVisible = SelectedNav == "Dashboard";
        IsIncidentsVisible = SelectedNav == "Incidents";
        IsVaultVisible = SelectedNav == "Vault";
        IsPoliciesVisible = SelectedNav == "Policies";
        IsDetectionsVisible = SelectedNav == "Detections";
        IsAttackLabVisible = SelectedNav == "AttackLab";
        IsSearchVisible = SelectedNav == "Search";
        IsSettingsVisible = SelectedNav == "Settings";
        IsHelpVisible = SelectedNav == "Help";
        IsPreferencesVisible = SelectedNav == "Preferences";
    }

    [RelayCommand]
    private void SelectScenario(string? scenario)
    {
        SelectedScenario = string.IsNullOrWhiteSpace(scenario) ? "usb-exfil" : scenario;
        SeedAttackSteps(SelectedScenario);
        SimulationResult = "";
        SimulationStatus = "Ready — press Run attack simulation.";
    }

    [RelayCommand]
    private async Task RunAttackSimulationAsync()
    {
        if (IsSimulationRunning || _disposed) return;
        IsSimulationRunning = true;
        SimulationResult = "";
        SeedAttackSteps(SelectedScenario);
        MarkAttackStep(0, "Running", "#F59E0B");
        SimulationStatus = "Injecting attacker signals into Guardian… watch steps and Dashboard.";

        try
        {
            var runTask = _guardian.RunAttackSimulationAsync(SelectedScenario);
            _ = AnimateAttackStepsAsync(SelectedScenario);

            var (ok, message) = await runTask.ConfigureAwait(true);
            await RefreshAsync().ConfigureAwait(true);

            if (ok)
            {
                CompleteAllAttackSteps();
                SimulationStatus = "Simulation finished — MayaJaal correlated the attack and applied policy.";
                SimulationResult = BuildFacultyResult(message);
                StatusBarText = message;
            }
            else
            {
                SimulationStatus = "Simulation failed.";
                SimulationResult = message;
                StatusBarText = message;
            }
        }
        finally
        {
            IsSimulationRunning = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_refreshing || _disposed) return;
        _refreshing = true;
        try
        {
            OnPropertyChanged(nameof(UptimeText));
            var snap = await _guardian.RefreshAsync().ConfigureAwait(true);
            ApplySnapshot(snap);
        }
        finally
        {
            _refreshing = false;
        }
    }

    [RelayCommand]
    private async Task LockVaultAsync()
    {
        var ok = await _guardian.LockVaultAsync().ConfigureAwait(true);
        VaultStatus = ok ? "Locked" : "Lock failed";
        StatusBarText = ok
            ? "All vaults locked successfully."
            : "Vault lock failed — Guardian may be offline.";
        HealthText = ok ? "Containment command sent" : "Action failed";
        HealthBrush = ok ? "#16A34A" : "#EF4444";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task UnlockVaultAsync()
    {
        var (ok, message) = await _guardian.UnlockVaultAsync(string.IsNullOrWhiteSpace(ActiveVaultId) ? null : ActiveVaultId)
            .ConfigureAwait(true);
        VaultStatus = ok ? "Unlocked" : "Unlock failed";
        StatusBarText = message;
        HealthText = ok ? "Vault ready for files" : "Unlock failed";
        HealthBrush = ok ? "#16A34A" : "#EF4444";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AddVaultFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Add file to vault",
            CheckFileExists = true,
            Multiselect = false,
            Filter = "All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        var (ok, message) = await _guardian
            .AddVaultItemAsync(dialog.FileName, string.IsNullOrWhiteSpace(ActiveVaultId) ? null : ActiveVaultId)
            .ConfigureAwait(true);

        StatusBarText = message;
        VaultStatus = ok ? "File encrypted into vault" : "Add file failed";
        HealthText = ok ? "File secured in vault" : "Add file failed";
        HealthBrush = ok ? "#16A34A" : "#EF4444";

        if (!ok && message.Contains("locked", StringComparison.OrdinalIgnoreCase))
            NextActionText = "Vault is locked — click Unlock vault, then Add file again.";

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ResolveIncidentAsync(string? incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return;
        var ok = await _guardian.ResolveIncidentAsync(incidentId).ConfigureAwait(true);
        StatusBarText = ok ? $"Resolved {incidentId}" : "Resolve failed";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RollbackIncidentAsync(string? incidentId)
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return;
        var ok = await _guardian.ExecuteRollbackAsync(incidentId).ConfigureAwait(true);
        StatusBarText = ok ? $"Rollback executed for {incidentId}" : "Rollback failed";
        await RefreshAsync();
    }

    private void ApplySnapshot(GuardianSnapshot snap)
    {
        IsOffline = !snap.Online;
        ConnectionStatus = snap.Online ? "ONLINE" : "OFFLINE";
        ConnectionBrush = snap.Online ? "#16A34A" : "#EF4444";
        LastUpdatedText = DateTime.Now.ToString("HH:mm:ss");

        if (!snap.Online || snap.Status is null)
        {
            ThreatLevelText = "OFFLINE";
            ThreatLevelBrush = "#EF4444";
            ThreatSubText = "Guardian IPC unavailable";
            RiskScoreText = "--";
            RiskBarValue = 0;
            RiskLevelText = "Unavailable";
            RiskLevelBrush = "#71717A";
            ConfidenceText = "--";
            ConfidenceBarValue = 0;
            ActiveIncidentsText = "0";
            IncidentSummaryText = "No active incidents";
            EventCountText = "0";
            IncidentPanelBrush = "#F4F4F5";
            HealthText = "Guardian offline";
            HealthBrush = "#EF4444";
            StatusBarText = snap.Error is { Length: > 0 }
                ? $"Guardian unreachable: {snap.Error}"
                : "Start MayaJaal.Guardian with --console to restore telemetry.";
            ClearIncidentPanel();
            RebuildIncidents(Array.Empty<Incident>(), null);
            RebuildVaults(Array.Empty<MayaJaal.Shared.IPC.VaultSummary>());
            RebuildPolicies(Array.Empty<MayaJaal.Shared.IPC.PolicySummary>());
            SettingsSummary = $"Pipe: {PipeName} · Offline — start Guardian with --console";
            UpdateDashboardVisuals(null, Array.Empty<SecurityEvent>(), null);
            UpdateGettingStarted(offline: true, hasVaults: false, hasEvents: false, hasIncident: false);
            return;
        }

        var status = snap.Status;
        ThreatLevelText = status.Level.ToString();
        ThreatLevelBrush = BrushForLevel(status.Level);
        ThreatSubText = DescribeLevel(status.Level);
        RiskScoreText = status.CurrentRisk.ToString();
        RiskBarValue = Math.Clamp(status.CurrentRisk, 0, 200);
        RiskLevelText = DescribeRiskBand(status.CurrentRisk);
        RiskLevelBrush = BrushForRisk(status.CurrentRisk);
        ConfidenceText = $"{status.Confidence:P0}";
        ConfidenceBarValue = Math.Clamp(status.Confidence * 100.0, 0, 100);
        EventCountText = status.EventCount.ToString();

        var incident = snap.ActiveIncident ?? status.ActiveIncident;
        ActiveIncidentsText = incident is null ? "0" : "1";
        IncidentSummaryText = incident is null
            ? "No active incidents"
            : (string.IsNullOrWhiteSpace(incident.Number) ? incident.Id : incident.Number);

        HealthText = status.Level >= ThreatLevel.HIGH ? "Threat response active" : "System operational";
        HealthBrush = status.Level >= ThreatLevel.HIGH ? "#EF4444" : "#16A34A";
        StatusBarText = status.Level >= ThreatLevel.HIGH
            ? $"Threat detected — risk {status.CurrentRisk}, confidence {status.Confidence:P0}"
            : $"Monitoring active · risk {status.CurrentRisk} · {status.EventCount} events in window";

        ApplyIncident(incident);
        RebuildEvents(snap.Events);
        RebuildIncidents(snap.Incidents, incident);
        RebuildVaults(snap.Vaults);
        RebuildPolicies(snap.Policies);
        SettingsSummary =
            $"Pipe: {PipeName} · Online: {snap.Online} · Vaults: {snap.Vaults.Count} · " +
            $"Incidents listed: {Incidents.Count} · Events in window: {status.EventCount} · Poll: 2s";
        UpdateDashboardVisuals(status, snap.Events, incident);
        UpdateGettingStarted(
            offline: false,
            hasVaults: HasVaults,
            hasEvents: snap.Events.Count > 0 || status.EventCount > 0,
            hasIncident: incident is not null);
    }

    private void UpdateGettingStarted(bool offline, bool hasVaults, bool hasEvents, bool hasIncident)
    {
        if (offline)
        {
            ShowGettingStarted = true;
            GettingStartedText =
                "Start here: run MayaJaal.Guardian with --console, then press Refresh. The green ONLINE badge means you are connected.";
            NextActionText = "Start Guardian (--console), then click Refresh.";
            return;
        }

        if (!hasVaults || !IsVaultUnlocked)
        {
            ShowGettingStarted = true;
            GettingStartedText =
                "Connected. Open Vault → Unlock vault → Add file. Encrypted files appear in the vault list below.";
            NextActionText = !hasVaults
                ? "Go to Vault and click Unlock vault (creates the default vault)."
                : "Vault is locked — click Unlock vault, then Add file.";
            return;
        }

        if (!hasEvents && !hasIncident)
        {
            ShowGettingStarted = true;
            GettingStartedText =
                "Vault is ready. Add sensitive files, or open Attack Lab to run a faculty demo simulation.";
            NextActionText = "Add a file in Vault, or open Attack Lab and press Run.";
            return;
        }

        ShowGettingStarted = hasIncident;
        GettingStartedText = hasIncident
            ? "Active incident detected. Review Incidents to Resolve or Rollback, or use Lock all vaults for containment."
            : "";
        NextActionText = hasIncident
            ? "Review the active incident, then Resolve or Rollback when finished."
            : "Monitoring live telemetry. Use Attack Lab anytime to demo a response.";
    }

    private void UpdateDashboardVisuals(ThreatState? status, IReadOnlyList<SecurityEvent> events, Incident? incident)
    {
        var eventCount = status?.EventCount ?? events.Count;
        var mediumPlus = events.Count(e => e.Severity >= EventSeverity.MEDIUM);
        var highPlus = events.Count(e => e.Severity >= EventSeverity.HIGH);
        var actionRequired = incident is not null &&
            incident.Status is Shared.Models.IncidentStatus.OPEN
                or Shared.Models.IncidentStatus.UNDER_ANALYSIS
                or Shared.Models.IncidentStatus.CONTAINMENT_PENDING
            ? 1
            : 0;
        var resolved = incident is not null &&
            incident.Status is Shared.Models.IncidentStatus.CONTAINED
                or Shared.Models.IncidentStatus.RESOLVED
                or Shared.Models.IncidentStatus.CLOSED
                or Shared.Models.IncidentStatus.ARCHIVED
            ? 1
            : 0;

        var risk = status?.CurrentRisk ?? 0;
        RebuildKpis(eventCount, mediumPlus, actionRequired + highPlus, resolved, risk);
        RebuildHealthDonut(mediumPlus, actionRequired + highPlus, resolved, eventCount, risk);
        RebuildAlertBars(events);
        RebuildMonthBars(events);
        RebuildCategoryTrends(events);
    }

    private void RebuildKpis(int eventCount, int investigating, int actionRequired, int resolved, int risk)
    {
        Kpis.Clear();
        Kpis.Add(MakeKpi("Events (window)", eventCount, "Live from Guardian", "#09090B"));
        Kpis.Add(MakeKpi("Medium+ severity", investigating, "Counted from current events", "#09090B"));
        Kpis.Add(MakeKpi("Needs attention", actionRequired, actionRequired > 0 ? "Open incident or high severity" : "Nothing waiting", "#09090B"));
        Kpis.Add(MakeKpi("Risk score", risk, "Current ThreatEngine risk", "#09090B"));
    }

    private static KpiCard MakeKpi(string label, int value, string detail, string valueBrush)
        => new()
        {
            Label = label,
            Value = value.ToString("N0", CultureInfo.InvariantCulture),
            TrendArrow = "",
            Trend = detail,
            TrendBrush = "#71717A",
            ValueBrush = valueBrush
        };

    private void RebuildHealthDonut(int investigating, int actionRequired, int resolved, int eventCount, int risk)
    {
        var issues = investigating + actionRequired;
        var contained = resolved;
        var quiet = Math.Max(0, eventCount - issues);
        if (eventCount == 0 && risk == 0 && issues == 0)
        {
            quiet = 1;
            issues = 0;
            contained = 0;
        }

        var total = Math.Max(1.0, issues + quiet + contained);
        var issuesPct = issues / total;
        var goodPct = quiet / total;
        var snoozedPct = contained / total;

        HealthIssuesPctText = $"{issuesPct:P0}";
        HealthGoodPctText = $"{goodPct:P0}";
        HealthSnoozedPctText = $"{snoozedPct:P0}";
        DonutCenterLabel = eventCount == 0 && risk == 0 ? "—" : $"{goodPct:P0}";
        DonutCenterSub = issues > 0 || risk >= 80 ? "At risk" : (eventCount == 0 ? "Idle" : "Healthy");

        const double cx = 75, cy = 75, rOuter = 60, rInner = 38;
        var a0 = -90.0;
        var a1 = a0 + issuesPct * 360.0;
        var a2 = a1 + goodPct * 360.0;
        var a3 = a2 + snoozedPct * 360.0;

        DonutIssuesPath = DonutSegment(cx, cy, rOuter, rInner, a0, a1);
        DonutGoodPath = DonutSegment(cx, cy, rOuter, rInner, a1, a2);
        DonutSnoozedPath = DonutSegment(cx, cy, rOuter, rInner, a2, a3);
    }

    private void RebuildAlertBars(IReadOnlyList<SecurityEvent> events)
    {
        var honey = events.Count(e => e.IsHoney || e.Type is EventType.HONEY_ACCESS or EventType.HONEY_MODIFY);
        var usb = events.Count(e => e.Type is EventType.USB_INSERT or EventType.USB_REMOVE or EventType.USB_FILE_ACCESS);
        var process = events.Count(e => e.Type is EventType.PROCESS_START or EventType.PROCESS_EXIT);
        var mass = events.Count(e => e.Type is EventType.MASS_FILE_ACTIVITY or EventType.RANSOMWARE_BEHAVIOR);
        var other = Math.Max(0, events.Count - honey - usb - process - mass);

        var max = Math.Max(1, new[] { honey, usb, process, mass, other }.Max());
        const double track = 220.0;

        AlertBars.Clear();
        AlertBars.Add(Bar("Honey", honey, max, track, "#18181B"));
        AlertBars.Add(Bar("Process", process, max, track, "#3F3F46"));
        AlertBars.Add(Bar("Mass copy", mass, max, track, "#52525B"));
        AlertBars.Add(Bar("USB", usb, max, track, "#71717A"));
        AlertBars.Add(Bar("Other", other, max, track, "#A1A1AA"));
    }

    private static AlertBar Bar(string category, int value, int max, double track, string brush)
    {
        var pct = value <= 0 ? 0 : value / (double)max;
        return new AlertBar
        {
            Category = category,
            ValueText = value.ToString(CultureInfo.InvariantCulture),
            WidthPct = pct * 100.0,
            BarWidth = value <= 0 ? 0 : Math.Max(8, pct * track),
            BarBrush = brush
        };
    }

    private void RebuildCategoryTrends(IReadOnlyList<SecurityEvent> events)
    {
        var honey = events.Count(e => e.IsHoney || e.Type is EventType.HONEY_ACCESS or EventType.HONEY_MODIFY);
        var usb = events.Count(e => e.Type is EventType.USB_INSERT or EventType.USB_REMOVE or EventType.USB_FILE_ACCESS);
        var process = events.Count(e => e.Type is EventType.PROCESS_START or EventType.PROCESS_EXIT);
        var mass = events.Count(e => e.Type is EventType.MASS_FILE_ACTIVITY or EventType.RANSOMWARE_BEHAVIOR);
        var other = Math.Max(0, events.Count - honey - usb - process - mass);
        var max = Math.Max(1, new[] { honey, usb, process, mass, other }.Max());
        const double track = 280.0;

        CategoryTrends.Clear();
        CategoryTrends.Add(new CategoryTrend { Category = "Honey", BarWidth = honey * track / max, BarBrush = "#18181B" });
        CategoryTrends.Add(new CategoryTrend { Category = "Process", BarWidth = process * track / max, BarBrush = "#3F3F46" });
        CategoryTrends.Add(new CategoryTrend { Category = "Mass copy", BarWidth = mass * track / max, BarBrush = "#52525B" });
        CategoryTrends.Add(new CategoryTrend { Category = "USB", BarWidth = usb * track / max, BarBrush = "#71717A" });
        CategoryTrends.Add(new CategoryTrend { Category = "Other", BarWidth = other * track / max, BarBrush = "#A1A1AA" });
    }

    private void RebuildMonthBars(IReadOnlyList<SecurityEvent> events)
    {
        // Hour-of-day histogram for the current live window (no fabricated history).
        var buckets = new int[12];
        foreach (var evt in events)
        {
            var local = evt.Timestamp.ToLocalTime();
            var slot = Math.Clamp(local.Hour / 2, 0, 11);
            buckets[slot]++;
        }

        var max = Math.Max(1, buckets.Max());
        const double maxH = 100.0;
        var labels = new[] { "0h", "2h", "4h", "6h", "8h", "10h", "12h", "14h", "16h", "18h", "20h", "22h" };

        MonthBars.Clear();
        for (var i = 0; i < 12; i++)
        {
            var h = buckets[i] <= 0 ? 0 : Math.Max(6, buckets[i] / (double)max * maxH);
            MonthBars.Add(new MonthBar
            {
                Month = labels[i],
                PrimaryHeight = h,
                SecondaryHeight = 0
            });
        }
    }

    private void SeedEmptyDashboard() => UpdateDashboardVisuals(null, Array.Empty<SecurityEvent>(), null);

    private void SeedAttackSteps(string scenario)
    {
        AttackSteps.Clear();
        if (scenario is "insider" or "insider-harvest")
        {
            ScenarioTitle = "Insider credential harvest";
            ScenarioNarrative =
                "A malicious insider opens a decoy credential file, copies sensitive-looking content, then performs bulk file activity to stage data for theft.";
            DefenseNarrative =
                "Honey access is a high-confidence deception signal. Combined with copy + mass activity, PolicyEngine escalates and SafetyGate can approve containment.";
            AttackSteps.Add(Step(1, "Attacker opens decoy credentials (honey file)", "MayaJaal raises HONEY_ACCESS with critical severity"));
            AttackSteps.Add(Step(2, "Attacker copies harvested content", "FILE_COPY counted toward exfil confidence"));
            AttackSteps.Add(Step(3, "Attacker stages mass file activity", "MASS_FILE_ACTIVITY correlates into an insider pattern"));
            AttackSteps.Add(Step(4, "Policy selects response action", "Incident opened; vault lockdown may execute if confidence gates pass"));
        }
        else
        {
            ScenarioTitle = "USB data exfiltration";
            ScenarioNarrative =
                "An attacker plugs in a USB drive, launches a suspicious exfil tool, touches a decoy (honey) file, then bulk-copies documents to removable media.";
            DefenseNarrative =
                "MayaJaal correlates USB + process + honey + mass-copy signals, raises risk/confidence, opens an incident, and can execute reversible vault lockdown.";
            AttackSteps.Add(Step(1, "Attacker inserts USB device", "USB_INSERT scored; removable media watched"));
            AttackSteps.Add(Step(2, "Attacker starts exfil-tool.exe", "Suspicious PROCESS_START increases risk"));
            AttackSteps.Add(Step(3, "Attacker opens a decoy honey file", "HONEY_ACCESS — strong deception signal"));
            AttackSteps.Add(Step(4, "Attacker mass-copies files to USB", "MASS_FILE_ACTIVITY completes USB_Process_MassCopy pattern"));
            AttackSteps.Add(Step(5, "MayaJaal contains the threat", "Policy + SafetyGate → incident + vault lockdown"));
        }
    }

    private static AttackStepRow Step(int n, string attack, string defense) => new()
    {
        Number = n.ToString(CultureInfo.InvariantCulture),
        AttackAction = attack,
        DefenseAction = defense,
        Status = "Pending",
        StatusBrush = "#71717A"
    };

    private async Task AnimateAttackStepsAsync(string scenario)
    {
        var delays = scenario is "insider" or "insider-harvest"
            ? new[] { 200, 700, 1200, 1700 }
            : new[] { 200, 700, 1200, 1700, 2300 };

        for (var i = 0; i < AttackSteps.Count && i < delays.Length; i++)
        {
            await Task.Delay(delays[i]).ConfigureAwait(true);
            if (!IsSimulationRunning) break;
            MarkAttackStep(i, "Running", "#F59E0B");
            if (i > 0) MarkAttackStep(i - 1, "Done", "#16A34A");
        }
    }

    private void MarkAttackStep(int index, string status, string brush)
    {
        if (index < 0 || index >= AttackSteps.Count) return;
        var old = AttackSteps[index];
        AttackSteps[index] = new AttackStepRow
        {
            Number = old.Number,
            AttackAction = old.AttackAction,
            DefenseAction = old.DefenseAction,
            Status = status,
            StatusBrush = brush
        };
    }

    private void CompleteAllAttackSteps()
    {
        for (var i = 0; i < AttackSteps.Count; i++)
            MarkAttackStep(i, "Done", "#16A34A");
    }

    private string BuildFacultyResult(string engineMessage)
    {
        var level = ThreatLevelText;
        var risk = RiskScoreText;
        var conf = ConfidenceText;
        var action = string.IsNullOrWhiteSpace(IncidentAction) || IncidentAction == "—"
            ? "see Dashboard / Incidents"
            : IncidentAction;
        var incident = string.IsNullOrWhiteSpace(IncidentNumber) || IncidentNumber == "—"
            ? "(pending refresh)"
            : IncidentNumber;

        return
            $"Faculty talking points\n" +
            $"• Attacker path: {ScenarioTitle}\n" +
            $"• Detected level: {level} · Risk {risk} · Confidence {conf}\n" +
            $"• Automated response: {action}\n" +
            $"• Incident: {incident}\n" +
            $"• Engine: {engineMessage}";
    }

    private static string DonutSegment(double cx, double cy, double rOuter, double rInner, double startDeg, double endDeg)
    {
        var sweep = endDeg - startDeg;
        if (Math.Abs(sweep) < 0.05)
            return "";

        var large = Math.Abs(sweep) > 180 ? 1 : 0;
        var startOuter = Polar(cx, cy, rOuter, startDeg);
        var endOuter = Polar(cx, cy, rOuter, endDeg);
        var startInner = Polar(cx, cy, rInner, endDeg);
        var endInner = Polar(cx, cy, rInner, startDeg);

        return string.Create(CultureInfo.InvariantCulture,
            $"M {startOuter.X:0.###},{startOuter.Y:0.###} A {rOuter:0.###},{rOuter:0.###} 0 {large} 1 {endOuter.X:0.###},{endOuter.Y:0.###} L {startInner.X:0.###},{startInner.Y:0.###} A {rInner:0.###},{rInner:0.###} 0 {large} 0 {endInner.X:0.###},{endInner.Y:0.###} Z");
    }

    private static (double X, double Y) Polar(double cx, double cy, double r, double deg)
    {
        var rad = deg * Math.PI / 180.0;
        return (cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    private void ApplyIncident(Incident? incident)
    {
        if (incident is null)
        {
            ClearIncidentPanel();
            return;
        }

        HasActiveIncident = true;
        IncidentNumber = string.IsNullOrWhiteSpace(incident.Number) ? incident.Id : incident.Number;
        IncidentSignal = string.IsNullOrWhiteSpace(incident.PrimarySignal) ? "—" : incident.PrimarySignal;
        IncidentStatus = incident.Status.ToString();
        IncidentStatusBrush = BrushForIncidentStatus(incident.Status);
        IncidentAction = incident.Action.ToString();
        IncidentActionBrush = BrushForAction(incident.Action);
        IncidentRisk = $"{incident.RiskScore} / {incident.Confidence:P0}";
        IncidentStarted = incident.StartTime.ToLocalTime().ToString("HH:mm:ss");
        IncidentPanelBrush = "#F4F4F5";
        IncidentTimelineText = incident.Timeline.Count == 0
            ? IncidentSignal
            : string.Join(" → ", incident.Timeline.Take(8).Select(t =>
                string.IsNullOrWhiteSpace(t.Description) ? t.EventType : t.Description));
    }

    private void ClearIncidentPanel()
    {
        HasActiveIncident = false;
        IncidentNumber = "—";
        IncidentSignal = "—";
        IncidentStatus = "—";
        IncidentStatusBrush = "#71717A";
        IncidentAction = "—";
        IncidentActionBrush = "#18181B";
        IncidentRisk = "—";
        IncidentStarted = "—";
        IncidentPanelBrush = "#F4F4F5";
        IncidentTimelineText = "No active incident.";
    }

    private void RebuildEvents(IReadOnlyList<SecurityEvent> events)
    {
        Events.Clear();
        foreach (var evt in events.Take(40))
        {
            Events.Add(new EventRow
            {
                Time = evt.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Type = evt.Type.ToString(),
                Severity = evt.Severity.ToString(),
                Path = Truncate(evt.Path, 72),
                Risk = evt.RiskContribution.ToString(),
                SeverityBrush = BrushForSeverity(evt.Severity),
                TypeBrush = BrushForEventType(evt),
                RowBrush = evt.IsHoney ? "#FEF2F2" : "#FFFFFF",
                IsHoney = evt.IsHoney
            });
        }
    }

    private void RebuildIncidents(IReadOnlyList<Incident> incidents, Incident? active)
    {
        Incidents.Clear();
        var source = incidents.Count > 0
            ? incidents
            : active is null ? Array.Empty<Incident>() : new[] { active };

        foreach (var item in source.Take(50))
        {
            Incidents.Add(new IncidentRow
            {
                Id = item.Id,
                Number = string.IsNullOrWhiteSpace(item.Number) ? item.Id : item.Number,
                Level = item.Level.ToString(),
                Status = item.Status.ToString(),
                Signal = item.PrimarySignal,
                Risk = item.RiskScore.ToString(),
                Started = item.StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                LevelBrush = BrushForLevel(item.Level)
            });
        }

        HasIncidents = Incidents.Count > 0;
    }

    private void RebuildVaults(IReadOnlyList<MayaJaal.Shared.IPC.VaultSummary> vaults)
    {
        VaultRows.Clear();
        VaultItemRows.Clear();
        foreach (var v in vaults)
        {
            VaultRows.Add(new VaultRow
            {
                Id = v.Id,
                Name = v.Name,
                State = v.State,
                ItemCount = v.ItemCount.ToString(),
                TotalSize = FormatBytes(v.TotalSize),
                OwnerId = v.OwnerId
            });

            foreach (var item in v.Items)
            {
                VaultItemRows.Add(new VaultItemRow
                {
                    VaultName = v.Name,
                    Name = item.Name,
                    Size = FormatBytes(item.Size),
                    OriginalPath = string.IsNullOrWhiteSpace(item.OriginalPath) ? "—" : item.OriginalPath,
                    AddedAt = item.AddedAt == default
                        ? "—"
                        : item.AddedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                });
            }
        }

        HasVaults = VaultRows.Count > 0;
        HasVaultItems = VaultItemRows.Count > 0;
        ActiveVaultId = vaults.FirstOrDefault()?.Id ?? "";
        IsVaultUnlocked = vaults.Any(v =>
            v.State.Contains("UNLOCK", StringComparison.OrdinalIgnoreCase));

        if (VaultRows.Count > 0)
            VaultStatus = IsVaultUnlocked ? "Unlocked — ready to add files" : "Locked — click Unlock vault";
        else
            VaultStatus = "No vault yet — click Unlock vault to create one";
    }

    private void RebuildPolicies(IReadOnlyList<MayaJaal.Shared.IPC.PolicySummary> policies)
    {
        PolicyRows.Clear();
        foreach (var p in policies)
        {
            PolicyRows.Add(new PolicyRow
            {
                Name = p.Name,
                Description = p.Description,
                DefaultAction = p.DefaultAction,
                Enabled = p.IsEnabled ? "Enabled" : "Disabled",
                Version = p.Version
            });
        }

        HasPolicies = PolicyRows.Count > 0;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }

    private static string FormatUptime(TimeSpan span)
        => $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";

    private static string DescribeRiskBand(int risk) => risk switch
    {
        <= 20 => "Normal",
        <= 50 => "Elevated",
        <= 80 => "Suspicious",
        <= 120 => "High risk",
        _ => "Critical"
    };

    private static string BrushForRisk(int risk) => risk switch
    {
        <= 20 => "#16A34A",
        <= 50 => "#18181B",
        <= 80 => "#F59E0B",
        <= 120 => "#F87171",
        _ => "#EF4444"
    };

    private static string BrushForLevel(ThreatLevel level) => level switch
    {
        ThreatLevel.SAFE => "#16A34A",
        ThreatLevel.LOW => "#52525B",
        ThreatLevel.MEDIUM => "#F59E0B",
        ThreatLevel.HIGH => "#F87171",
        ThreatLevel.CRITICAL => "#EF4444",
        _ => "#71717A"
    };

    private static string DescribeLevel(ThreatLevel level) => level switch
    {
        ThreatLevel.SAFE => "All systems normal",
        ThreatLevel.LOW => "Low-risk activity detected",
        ThreatLevel.MEDIUM => "Suspicious activity in progress",
        ThreatLevel.HIGH => "High-risk threat detected",
        ThreatLevel.CRITICAL => "Critical threat — containment active",
        _ => "Unknown"
    };

    private static string BrushForSeverity(EventSeverity severity) => severity switch
    {
        EventSeverity.INFO => "#71717A",
        EventSeverity.LOW => "#16A34A",
        EventSeverity.MEDIUM => "#F59E0B",
        EventSeverity.HIGH => "#F87171",
        EventSeverity.CRITICAL => "#EF4444",
        _ => "#71717A"
    };

    private static string BrushForEventType(SecurityEvent evt)
    {
        if (evt.IsHoney || evt.Type is EventType.HONEY_ACCESS or EventType.HONEY_MODIFY)
            return "#EF4444";
        return evt.Type switch
        {
            EventType.USB_INSERT or EventType.USB_REMOVE or EventType.USB_FILE_ACCESS => "#F59E0B",
            EventType.PROCESS_START or EventType.PROCESS_EXIT => "#3F3F46",
            EventType.MASS_FILE_ACTIVITY or EventType.RANSOMWARE_BEHAVIOR => "#F87171",
            _ => "#18181B"
        };
    }

    private static string BrushForAction(ResponseAction action) => action switch
    {
        ResponseAction.EMERGENCY_LOCKDOWN or ResponseAction.CONTAIN => "#EF4444",
        ResponseAction.ALERT or ResponseAction.NOTIFY_USER => "#F59E0B",
        _ => "#18181B"
    };

    private static string BrushForIncidentStatus(Shared.Models.IncidentStatus status) => status switch
    {
        Shared.Models.IncidentStatus.CONTAINED => "#16A34A",
        Shared.Models.IncidentStatus.RESOLVED or Shared.Models.IncidentStatus.CLOSED => "#18181B",
        Shared.Models.IncidentStatus.OPEN or Shared.Models.IncidentStatus.UNDER_ANALYSIS
            or Shared.Models.IncidentStatus.CONTAINMENT_PENDING => "#F59E0B",
        _ => "#71717A"
    };

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "—";
        return value.Length <= max ? value : value[..(max - 1)] + "…";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _clockTimer.Stop();
        _guardian.Dispose();
    }
}

public sealed class KpiCard
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "";
    public string TrendArrow { get; init; } = "▲";
    public string Trend { get; init; } = "";
    public string TrendBrush { get; init; } = "#16A34A";
    public string ValueBrush { get; init; } = "#09090B";
}

public sealed class AlertBar
{
    public string Category { get; init; } = "";
    public string ValueText { get; init; } = "";
    public double WidthPct { get; init; }
    public double BarWidth { get; init; }
    public string BarBrush { get; init; } = "#18181B";
}

public sealed class CategoryTrend
{
    public string Category { get; init; } = "";
    public double BarWidth { get; set; }
    public string BarBrush { get; init; } = "#18181B";
}

public sealed class MonthBar
{
    public string Month { get; init; } = "";
    public double PrimaryHeight { get; init; }
    public double SecondaryHeight { get; init; }
}

public sealed class EventRow
{
    public string Time { get; init; } = "";
    public string Type { get; init; } = "";
    public string Severity { get; init; } = "";
    public string Path { get; init; } = "";
    public string Risk { get; init; } = "";
    public string SeverityBrush { get; init; } = "#71717A";
    public string TypeBrush { get; init; } = "#18181B";
    public string RowBrush { get; init; } = "#FFFFFF";
    public bool IsHoney { get; init; }
}

public sealed class IncidentRow
{
    public string Id { get; init; } = "";
    public string Number { get; init; } = "";
    public string Level { get; init; } = "";
    public string Status { get; init; } = "";
    public string Signal { get; init; } = "";
    public string Risk { get; init; } = "";
    public string Started { get; init; } = "";
    public string LevelBrush { get; init; } = "#71717A";
}

public sealed class VaultRow
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string State { get; init; } = "";
    public string ItemCount { get; init; } = "0";
    public string TotalSize { get; init; } = "0 B";
    public string OwnerId { get; init; } = "";
}

public sealed class VaultItemRow
{
    public string VaultName { get; init; } = "";
    public string Name { get; init; } = "";
    public string Size { get; init; } = "0 B";
    public string OriginalPath { get; init; } = "";
    public string AddedAt { get; init; } = "";
}

public sealed class PolicyRow
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string DefaultAction { get; init; } = "";
    public string Enabled { get; init; } = "";
    public string Version { get; init; } = "";
}

public sealed class AttackStepRow
{
    public string Number { get; init; } = "";
    public string AttackAction { get; init; } = "";
    public string DefenseAction { get; init; } = "";
    public string Status { get; init; } = "Pending";
    public string StatusBrush { get; init; } = "#71717A";
}

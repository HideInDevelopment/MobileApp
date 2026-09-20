using System.Collections.ObjectModel;
using System.Globalization;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Application.Entitlements;
using Anthropometry.App.Display;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Measurements;

public enum MetricKind
{
    Weight,
    BodyFatPercentage,
    BasalMetabolicRate,
    TotalDailyEnergyExpenditure
}

public sealed record MetricOption(MetricKind Value, string DisplayName, bool IsLocked = false)
{
    public string Code => Value.ToString();
}

public sealed record WeightGraphicPoint(
    DateTimeOffset MeasuredAtUtc,
    decimal WeightKg,
    decimal DisplayedWeight,
    string DateText,
    decimal? MetricValue = null,
    decimal? DisplayedMetricValue = null,
    string MetricUnit = "kg",
    CalculationType? MetricCalculationType = null)
{
    public MeasurementId MeasurementId { get; init; }

    public decimal Value => MetricValue ?? WeightKg;

    public decimal DisplayedValue => DisplayedMetricValue ?? DisplayedWeight;

    public string Unit => MetricUnit;

    public CalculationType? CalculationType => MetricCalculationType;
}

public sealed class WeightGraphicViewModel : ObservableObject
{
    private const double ChartPointSpacing = 40d;

    private readonly GetMetricHistory _getHistory;
    private readonly ProfileId _profileId;
    private readonly LanguageService _languageService;
    private readonly DisplayPreferencesService _displayPreferences;
    private readonly EntitledDisplayPreferences _entitledDisplayPreferences;
    private readonly ObservableCollection<WeightGraphicPoint> _points = [];
    private bool _isLoading;
    private string? _errorMessage;
    private WeightGraphicPoint? _selectedPoint;
    private MetricKind _selectedMetric = MetricKind.Weight;
    private MetricOption? _selectedMetricOption;
    private bool _useFromDate;
    private bool _useToDate;
    private DateTime _fromDate = DateTime.Today.AddDays(-30);
    private DateTime _toDate = DateTime.Today;
    private bool _suppressFilterReload;
    private readonly bool _isFullGraphicsLocked;
    private readonly Func<Task>? _showPremiumAsync;
    private bool _isMetricMenuVisible;

    public WeightGraphicViewModel(
        GetMetricHistory getHistory,
        ProfileId profileId,
        LanguageService languageService,
        DisplayPreferencesService displayPreferences,
        EntitlementSnapshot? entitlement = null,
        Func<Task>? showPremiumAsync = null)
    {
        _getHistory = getHistory;
        _profileId = profileId;
        _languageService = languageService;
        _displayPreferences = displayPreferences;
        var currentEntitlement = entitlement
            ?? new EntitlementSnapshot(EntitlementTier.Free, SubscriptionState.Active, null, null, null);
        _entitledDisplayPreferences = new EntitledDisplayPreferences(_displayPreferences, currentEntitlement);
        _isFullGraphicsLocked = !FeatureAccessPolicy.CanUse(currentEntitlement, PremiumFeature.FullMeasurementGraphics);
        _showPremiumAsync = showPremiumAsync;
        Points = new ReadOnlyObservableCollection<WeightGraphicPoint>(_points);
        MetricOptions = CreateMetricOptions();
        _selectedMetricOption = MetricOptions[0];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ClearDateRangeCommand = new AsyncRelayCommand(ClearDateRangeAsync);
        SelectMetricCommand = new RelayCommand<string?>(SelectMetric);
        ToggleMetricMenuCommand = new RelayCommand(() => IsMetricMenuVisible = !IsMetricMenuVisible);
        DismissMetricMenuCommand = new RelayCommand(() => IsMetricMenuVisible = false);
        _displayPreferences.PreferencesChanged += OnDisplayPreferencesChanged;
        _languageService.LanguageChanged += OnLanguageChanged;
    }

    public ReadOnlyObservableCollection<WeightGraphicPoint> Points { get; }

    public IReadOnlyList<MetricOption> MetricOptions { get; private set; }

    public bool IsFullGraphicsLocked => _isFullGraphicsLocked;

    public bool IsMetricMenuVisible
    {
        get => _isMetricMenuVisible;
        private set => SetProperty(ref _isMetricMenuVisible, value);
    }

    public string PremiumGraphicsText => _languageService.Get("PremiumRequired");

    public MetricKind SelectedMetric
    {
        get => _selectedMetric;
        set
        {
            if (!SetProperty(ref _selectedMetric, value))
            {
                return;
            }

            if (MetricOptions.All(option => option.Value != value))
            {
                _selectedMetric = MetricKind.Weight;
                _selectedMetricOption = MetricOptions[0];
                OnPropertyChanged(nameof(SelectedMetric));
                OnPropertyChanged(nameof(SelectedMetricOption));
                return;
            }

            _selectedMetricOption = MetricOptions.First(option => option.Value == value);
            OnPropertyChanged(nameof(SelectedMetricOption));
            NotifyMetricChanged();
            ScheduleFilterReload();
        }
    }

    public MetricOption? SelectedMetricOption
    {
        get => _selectedMetricOption;
        set
        {
            if (!SetProperty(ref _selectedMetricOption, value))
            {
                return;
            }

            if (value is not null && _selectedMetric != value.Value)
            {
                _selectedMetric = value.Value;
                OnPropertyChanged(nameof(SelectedMetric));
            }

            NotifyMetricChanged();
            ScheduleFilterReload();
        }
    }

    public bool UseFromDate
    {
        get => _useFromDate;
        set
        {
            if (SetProperty(ref _useFromDate, value))
            {
                NotifyDateFilterChanged();
                ScheduleFilterReload();
            }
        }
    }

    public bool UseToDate
    {
        get => _useToDate;
        set
        {
            if (SetProperty(ref _useToDate, value))
            {
                NotifyDateFilterChanged();
                ScheduleFilterReload();
            }
        }
    }

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value.Date))
            {
                ScheduleFilterReload();
            }
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value.Date))
            {
                ScheduleFilterReload();
            }
        }
    }

    public bool HasDateFilters => UseFromDate || UseToDate;

    public string MetricAxisLabel => _selectedMetricOption?.DisplayName ?? _languageService.Get("Weight");

    public string ValueAxisLabel => MetricAxisLabel;

    public string WeightAxisLabel => MetricAxisLabel;

    public string DateAxisLabel => _languageService.Get("Date");

    public double ChartWidth => Math.Max(360d, Points.Count * ChartPointSpacing + 72d);

    public bool IsLegendVisible => _selectedPoint is not null;

    public string LegendText => _selectedPoint is null
        ? string.Empty
        : string.Format(
            CultureInfo.CurrentCulture,
            "{0}: {1}{2}{3}: {4} {5}",
            DateAxisLabel,
            _entitledDisplayPreferences.FormatDate(_selectedPoint.MeasuredAtUtc),
            Environment.NewLine,
            MetricAxisLabel,
            _selectedPoint.DisplayedValue.ToString("0.##", CultureInfo.CurrentCulture),
            _selectedPoint.Unit);

    public decimal ChartMinimumValue => Points.Count == 0
        ? 0m
        : Points.Min(point => point.DisplayedValue) - ChartPadding;

    public decimal ChartMaximumValue => Points.Count == 0
        ? 0m
        : Points.Max(point => point.DisplayedValue) + ChartPadding;

    public decimal ChartMinimumWeight => ChartMinimumValue;

    public decimal ChartMaximumWeight => ChartMaximumValue;

    public bool HasPoints => Points.Count > 0;

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                NotifyEmptyStateChanged();
            }
        }
    }

    public bool IsEmpty => !IsLoading && Points.Count == 0 && ErrorMessage is null;

    public string EmptyStateText => _languageService.Get("MetricChartNoMeasurements");

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                NotifyEmptyStateChanged();
            }
        }
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand ClearDateRangeCommand { get; }

    public IRelayCommand<string?> SelectMetricCommand { get; }

    public IRelayCommand ToggleMetricMenuCommand { get; }

    public IRelayCommand DismissMetricMenuCommand { get; }

    public void SelectPoint(WeightGraphicPoint? point)
    {
        if (EqualityComparer<WeightGraphicPoint?>.Default.Equals(_selectedPoint, point))
        {
            return;
        }

        _selectedPoint = point;
        OnPropertyChanged(nameof(IsLegendVisible));
        OnPropertyChanged(nameof(LegendText));
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SelectPoint(null);
        _points.Clear();

        try
        {
            var result = await _getHistory.ExecuteAsync(BuildQuery(), CancellationToken.None);
            if (result.IsSuccess)
            {
                foreach (var metric in result.Value)
                {
                    _points.Add(CreatePoint(metric));
                }
            }
            else
            {
                ErrorMessage = result.Error!.Code == "metricHistory.dateRange.invalid"
                    ? _languageService.Get("InvalidMetricChartDateRange")
                    : _languageService.Get("MetricChartLoadError");
            }
        }
        finally
        {
            IsLoading = false;
            NotifyChartChanged();
            NotifyEmptyStateChanged();
        }
    }

    private MetricHistoryQuery BuildQuery()
        => new(
            _profileId,
            ToCalculationType(SelectedMetric),
            UseFromDate ? ToUtcStart(FromDate) : null,
            UseToDate ? ToUtcEnd(ToDate) : null);

    private static CalculationType? ToCalculationType(MetricKind metric)
        => metric switch
        {
            MetricKind.Weight => null,
            MetricKind.BodyFatPercentage => CalculationType.BodyFatPercentage,
            MetricKind.BasalMetabolicRate => CalculationType.BasalMetabolicRate,
            MetricKind.TotalDailyEnergyExpenditure => CalculationType.TotalDailyEnergyExpenditure,
            _ => null
        };

    private static DateTimeOffset ToUtcStart(DateTime date)
    {
        var local = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }

    private static DateTimeOffset ToUtcEnd(DateTime date)
    {
        var local = DateTime.SpecifyKind(date.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }

    private async Task ClearDateRangeAsync()
    {
        _suppressFilterReload = true;
        try
        {
            UseFromDate = false;
            UseToDate = false;
        }
        finally
        {
            _suppressFilterReload = false;
        }

        await LoadAsync();
    }

    private void SelectMetric(string? value)
    {
        if (Enum.TryParse<MetricKind>(value, ignoreCase: true, out var metric))
        {
            var option = MetricOptions.FirstOrDefault(candidate => candidate.Value == metric);
            IsMetricMenuVisible = false;
            if (option is null)
            {
                return;
            }

            if (option.IsLocked)
            {
                _ = _showPremiumAsync?.Invoke();
                return;
            }

            SelectedMetric = metric;
        }
    }

    private void ScheduleFilterReload()
    {
        if (!_suppressFilterReload)
        {
            _ = LoadAsync();
        }
    }

    private List<MetricOption> CreateMetricOptions()
    {
        return
        [
            new(MetricKind.Weight, _languageService.Get("Weight")),
            new(MetricKind.BodyFatPercentage, _languageService.Get("BodyFatPercentage"), _isFullGraphicsLocked),
            new(MetricKind.BasalMetabolicRate, _languageService.Get("BasalMetabolicRate"), _isFullGraphicsLocked),
            new(MetricKind.TotalDailyEnergyExpenditure, _languageService.Get("TotalDailyEnergyExpenditure"), _isFullGraphicsLocked)
        ];
    }

    private WeightGraphicPoint CreatePoint(MetricHistoryDto metric)
    {
        var displayedValue = metric.CalculationType is null
            ? _entitledDisplayPreferences.ToDisplayWeight(metric.Value)
            : metric.Value;
        var unit = metric.CalculationType is null
            ? _languageService.Get(_entitledDisplayPreferences.WeightUnitCode == DisplayPreferencesService.PoundsCode ? "Lb" : "Kg")
            : metric.Unit;
        return new WeightGraphicPoint(
            metric.MeasuredAtUtc,
            metric.Value,
            displayedValue,
            _entitledDisplayPreferences.FormatCompactDate(metric.MeasuredAtUtc),
            metric.Value,
            displayedValue,
            unit,
            metric.CalculationType)
        {
            MeasurementId = metric.MeasurementId
        };
    }

    private decimal ChartPadding => SelectedMetric == MetricKind.Weight
        ? _entitledDisplayPreferences.ToDisplayWeight(10m)
        : 10m;

    private void OnDisplayPreferencesChanged(object? sender, EventArgs e)
    {
        RefreshPointDisplay();
        SelectPoint(null);
        OnPropertyChanged(nameof(ChartMinimumValue));
        OnPropertyChanged(nameof(ChartMaximumValue));
        OnPropertyChanged(nameof(ChartMinimumWeight));
        OnPropertyChanged(nameof(ChartMaximumWeight));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedMetric = SelectedMetric;
        MetricOptions = CreateMetricOptions();
        _selectedMetric = MetricOptions.Any(option => option.Value == selectedMetric)
            ? selectedMetric
            : MetricKind.Weight;
        _selectedMetricOption = MetricOptions.First(option => option.Value == _selectedMetric);
        OnPropertyChanged(nameof(MetricOptions));
        OnPropertyChanged(nameof(SelectedMetricOption));
        NotifyMetricChanged();
        RefreshPointDisplay();
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(LegendText));
    }

    private void RefreshPointDisplay()
    {
        if (_points.Count == 0)
        {
            return;
        }

        var canonicalPoints = _points.Select(point => new
        {
            point.MeasurementId,
            point.MeasuredAtUtc,
            point.WeightKg,
            point.MetricValue,
            point.MetricCalculationType,
            point.MetricUnit
        }).ToArray();
        _points.Clear();
        foreach (var point in canonicalPoints)
        {
            var value = point.MetricValue ?? point.WeightKg;
            var displayedValue = point.MetricCalculationType is null
                ? _entitledDisplayPreferences.ToDisplayWeight(value)
                : value;
            var unit = point.MetricCalculationType is null
                ? _languageService.Get(_entitledDisplayPreferences.WeightUnitCode == DisplayPreferencesService.PoundsCode ? "Lb" : "Kg")
                : point.MetricUnit;
            _points.Add(new WeightGraphicPoint(
                point.MeasuredAtUtc,
                point.WeightKg,
                displayedValue,
                _entitledDisplayPreferences.FormatCompactDate(point.MeasuredAtUtc),
                value,
                displayedValue,
                unit,
                point.MetricCalculationType)
            {
                MeasurementId = point.MeasurementId
            });
        }

        NotifyChartChanged();
    }

    private void NotifyMetricChanged()
    {
        OnPropertyChanged(nameof(MetricAxisLabel));
        OnPropertyChanged(nameof(ValueAxisLabel));
        OnPropertyChanged(nameof(WeightAxisLabel));
        OnPropertyChanged(nameof(LegendText));
        OnPropertyChanged(nameof(ChartMinimumValue));
        OnPropertyChanged(nameof(ChartMaximumValue));
        OnPropertyChanged(nameof(ChartMinimumWeight));
        OnPropertyChanged(nameof(ChartMaximumWeight));
    }

    private void NotifyDateFilterChanged()
        => OnPropertyChanged(nameof(HasDateFilters));

    private void NotifyChartChanged()
    {
        OnPropertyChanged(nameof(HasPoints));
        OnPropertyChanged(nameof(ChartWidth));
        OnPropertyChanged(nameof(ChartMinimumValue));
        OnPropertyChanged(nameof(ChartMaximumValue));
        OnPropertyChanged(nameof(ChartMinimumWeight));
        OnPropertyChanged(nameof(ChartMaximumWeight));
    }

    private void NotifyEmptyStateChanged()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
    }
}

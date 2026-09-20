using System.Collections.ObjectModel;
using System.Globalization;
using Anthropometry.Application.Common;
using Anthropometry.Application.Entitlements;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Display;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Measurements;

public sealed class MeasurementHistoryViewModel : ObservableObject
{
    private readonly GetMeasurementHistory _getHistory;
    private readonly DeleteMeasurement _deleteMeasurement;
    private readonly ProfileDto _profile;
    private readonly IMeasurementNavigation _navigation;
    private readonly LanguageService _languageService;
    private readonly DisplayPreferencesService _displayPreferences;
    private readonly EntitledDisplayPreferences _entitledDisplayPreferences;
    private readonly ObservableCollection<MeasurementHistoryItem> _measurements = [];
    private bool _isLoading;
    private string? _errorMessage;
    private bool _isFilterPanelVisible;
    private bool _useFromDate;
    private bool _useToDate;
    private DateTime _fromDate = DateTime.Today;
    private DateTime _toDate = DateTime.Today;
    private MeasurementType? _selectedType;
    private MeasurementTypeFilterOption? _selectedTypeOption;
    private bool _suppressFilterReload;
    private bool _isChartMenuVisible;

    public MeasurementHistoryViewModel(
        GetMeasurementHistory getHistory,
        DeleteMeasurement deleteMeasurement,
        ProfileDto profile,
        IMeasurementNavigation navigation,
        LanguageService languageService,
        DisplayPreferencesService displayPreferences,
        EntitlementSnapshot? entitlement = null)
    {
        _getHistory = getHistory;
        _deleteMeasurement = deleteMeasurement;
        _profile = profile;
        _navigation = navigation;
        _languageService = languageService;
        _displayPreferences = displayPreferences;
        _entitledDisplayPreferences = new EntitledDisplayPreferences(
            _displayPreferences,
            entitlement ?? new EntitlementSnapshot(EntitlementTier.Free, SubscriptionState.Active, null, null, null));
        Measurements = new ReadOnlyObservableCollection<MeasurementHistoryItem>(_measurements);
        MeasurementTypeOptions = CreateMeasurementTypeOptions();
        _selectedTypeOption = MeasurementTypeOptions[0];
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SelectCommand = new AsyncRelayCommand<MeasurementHistoryItem?>(SelectAsync);
        EditCommand = new AsyncRelayCommand<MeasurementHistoryItem?>(EditAsync);
        DeleteCommand = new AsyncRelayCommand<MeasurementHistoryItem?>(DeleteAsync);
        ChartsCommand = new RelayCommand(ToggleChartMenu);
        WeightGraphicCommand = new AsyncRelayCommand(ShowWeightGraphicAsync);
        DismissChartMenuCommand = new RelayCommand(() => IsChartMenuVisible = false);
        SelectMeasurementTypeCommand = new RelayCommand<string?>(SelectMeasurementType);
        ToggleFiltersCommand = new RelayCommand(() => IsFilterPanelVisible = !IsFilterPanelVisible);
        ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);
        _displayPreferences.PreferencesChanged += OnDisplayPreferencesChanged;
        _languageService.LanguageChanged += OnLanguageChanged;
    }

    public ReadOnlyObservableCollection<MeasurementHistoryItem> Measurements { get; }

    public IReadOnlyList<MeasurementTypeFilterOption> MeasurementTypeOptions { get; private set; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsFilterPanelVisible
    {
        get => _isFilterPanelVisible;
        set => SetProperty(ref _isFilterPanelVisible, value);
    }

    public bool IsChartMenuVisible
    {
        get => _isChartMenuVisible;
        private set => SetProperty(ref _isChartMenuVisible, value);
    }

    public bool UseFromDate
    {
        get => _useFromDate;
        set
        {
            if (SetProperty(ref _useFromDate, value))
            {
                FilterChanged();
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
                FilterChanged();
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
                FilterChanged();
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
                FilterChanged();
            }
        }
    }

    public MeasurementType? SelectedType
    {
        get => _selectedType;
        set
        {
            if (!SetProperty(ref _selectedType, value))
            {
                return;
            }

            _selectedTypeOption = MeasurementTypeOptions.First(option => option.Value == value);
            OnPropertyChanged(nameof(SelectedTypeOption));
            FilterChanged();
        }
    }

    public MeasurementTypeFilterOption? SelectedTypeOption
    {
        get => _selectedTypeOption;
        set
        {
            if (!SetProperty(ref _selectedTypeOption, value))
            {
                return;
            }

            if (_selectedType != value?.Value)
            {
                _selectedType = value?.Value;
                OnPropertyChanged(nameof(SelectedType));
            }

            FilterChanged();
        }
    }

    public bool HasActiveFilters => UseFromDate || UseToDate || SelectedType.HasValue;

    public bool IsEmpty => !IsLoading && Measurements.Count == 0 && ErrorMessage is null && !HasActiveFilters;

    public bool IsNoMatch => !IsLoading && Measurements.Count == 0 && ErrorMessage is null && HasActiveFilters;

    public string EmptyStateTitle => _languageService.Get(HasActiveFilters ? "NoMatchingMeasurements" : "NoMeasurements");

    public string EmptyStateDescription => _languageService.Get(HasActiveFilters ? "NoMatchingMeasurementsDescription" : "SavedMeasurementsLocal");

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

    public IAsyncRelayCommand<MeasurementHistoryItem?> SelectCommand { get; }

    public IAsyncRelayCommand<MeasurementHistoryItem?> EditCommand { get; }

    public IAsyncRelayCommand<MeasurementHistoryItem?> DeleteCommand { get; }

    public IRelayCommand ChartsCommand { get; }

    public IAsyncRelayCommand WeightGraphicCommand { get; }

    public IRelayCommand DismissChartMenuCommand { get; }

    public IRelayCommand<string?> SelectMeasurementTypeCommand { get; }

    public IRelayCommand ToggleFiltersCommand { get; }

    public IAsyncRelayCommand ClearFiltersCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _getHistory.ExecuteAsync(BuildQuery(), CancellationToken.None);
            _measurements.Clear();
            if (result.IsSuccess)
            {
                foreach (var measurement in result.Value)
                {
                    _measurements.Add(CreateItem(measurement));
                }
            }
            else
            {
                ErrorMessage = result.Error!.Code == "measurementHistory.dateRange.invalid"
                    ? _languageService.Get("InvalidHistoryDateRange")
                    : _languageService.Get("LoadHistoryError");
            }
        }
        finally
        {
            IsLoading = false;
            NotifyEmptyStateChanged();
        }
    }

    private MeasurementHistoryQuery BuildQuery()
        => new(
            _profile.Id,
            UseFromDate ? ToUtcStart(FromDate) : null,
            UseToDate ? ToUtcEnd(ToDate) : null,
            SelectedType);

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

    private Task SelectAsync(MeasurementHistoryItem? item)
        => item is null || !item.CanViewResults
            ? Task.CompletedTask
            : _navigation.ShowResultsAsync(item.Measurement);

    private void ToggleChartMenu()
        => IsChartMenuVisible = !IsChartMenuVisible;

    private void SelectMeasurementType(string? value)
    {
        var selectedType = string.IsNullOrWhiteSpace(value)
            ? (MeasurementType?)null
            : Enum.TryParse<MeasurementType>(value, ignoreCase: true, out var parsedType)
                ? parsedType
                : null;

        SelectedTypeOption = MeasurementTypeOptions.FirstOrDefault(option => option.Value == selectedType);
    }

    private async Task ShowWeightGraphicAsync()
    {
        IsChartMenuVisible = false;
        await _navigation.ShowWeightGraphicAsync(_profile.Id);
    }

    private Task EditAsync(MeasurementHistoryItem? item)
        => item is null
            ? Task.CompletedTask
            : _navigation.EditMeasurementAsync(_profile, item.Measurement);

    private async Task DeleteAsync(MeasurementHistoryItem? item)
    {
        if (item is null || !await _navigation.ConfirmDeleteAsync(item.Measurement))
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _deleteMeasurement.ExecuteAsync(_profile.Id, item.Measurement.Id, CancellationToken.None);
            if (!result.IsSuccess)
            {
                ErrorMessage = _languageService.Get("DeleteMeasurementError");
                return;
            }

            await LoadAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearFiltersAsync()
    {
        _suppressFilterReload = true;
        try
        {
            UseFromDate = false;
            UseToDate = false;
            SelectedTypeOption = MeasurementTypeOptions[0];
        }
        finally
        {
            _suppressFilterReload = false;
        }

        await LoadAsync();
    }

    private void FilterChanged()
    {
        OnPropertyChanged(nameof(HasActiveFilters));
        NotifyEmptyStateChanged();
        if (!_suppressFilterReload)
        {
            _ = LoadAsync();
        }
    }

    private IReadOnlyList<MeasurementTypeFilterOption> CreateMeasurementTypeOptions()
        =>
        [
            new(null, _languageService.Get("AllMeasurements")),
            new(MeasurementType.WeightAndSizes, _languageService.Get("WeightAndSizes")),
            new(MeasurementType.WeightOnly, _languageService.Get("WeightOnly"))
        ];

    private MeasurementHistoryItem CreateItem(MeasurementDto measurement)
        => new(
            measurement,
            _entitledDisplayPreferences.FormatDate(measurement.MeasuredAtUtc),
            measurement.Type == MeasurementType.WeightOnly
                ? _languageService.Get("WeightOnly")
                : _languageService.Get("WeightAndSizes"),
            string.Format(
                CultureInfo.CurrentCulture,
                "{0}: {1:0.##} {2}",
                _languageService.Get("Weight"),
                _entitledDisplayPreferences.ToDisplayWeight(measurement.WeightKg),
                _languageService.Get(_entitledDisplayPreferences.WeightUnitCode == DisplayPreferencesService.PoundsCode ? "Lb" : "Kg")),
            string.Format(
                CultureInfo.CurrentCulture,
                "{0}: {1:0.##} {2}",
                _languageService.Get("Height"),
                _entitledDisplayPreferences.ToDisplayHeight(measurement.HeightCm),
                _languageService.Get(_entitledDisplayPreferences.HeightUnitCode == DisplayPreferencesService.FeetCode ? "Ft" : "M")));

    private void OnDisplayPreferencesChanged(object? sender, EventArgs e)
        => RefreshItems();

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedType = SelectedType;
        MeasurementTypeOptions = CreateMeasurementTypeOptions();
        _selectedTypeOption = MeasurementTypeOptions.First(option => option.Value == selectedType);
        OnPropertyChanged(nameof(MeasurementTypeOptions));
        OnPropertyChanged(nameof(SelectedTypeOption));
        NotifyEmptyStateChanged();
        RefreshItems();
    }

    private void RefreshItems()
    {
        if (_measurements.Count == 0)
        {
            NotifyEmptyStateChanged();
            return;
        }

        var measurements = _measurements.Select(item => item.Measurement).ToArray();
        _measurements.Clear();
        foreach (var measurement in measurements)
        {
            _measurements.Add(CreateItem(measurement));
        }
    }

    private void NotifyEmptyStateChanged()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsNoMatch));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
    }
}

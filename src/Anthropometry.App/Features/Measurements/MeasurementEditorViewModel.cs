using System.Globalization;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Entitlements;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Display;
using Anthropometry.App.Features.Help;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Measurements;

public sealed class MeasurementEditorViewModel : ObservableObject
{
    private readonly RecordMeasurement _recordMeasurement;
    private readonly UpdateMeasurement? _updateMeasurement;
    private readonly CalculateBodyFat _calculateBodyFat;
    private readonly CalculateBasalMetabolicRate _calculateBmr;
    private readonly CalculateTotalDailyEnergyExpenditure _calculateTdee;
    private readonly ProfileDto _profile;
    private readonly MeasurementType _measurementType;
    private readonly IMeasurementNavigation _navigation;
    private readonly LanguageService _languageService;
    private readonly DisplayPreferencesService _displayPreferences;
    private readonly EntitledDisplayPreferences _entitledDisplayPreferences;
    private readonly IEntitlementProvider _entitlementProvider;
    private readonly IClock? _clock;
    private readonly MeasurementDto? _existingMeasurement;
    private string _weightText = string.Empty;
    private string _neckText = string.Empty;
    private string _abdomenText = string.Empty;
    private string _hipText = string.Empty;
    private bool _isBusy;
    private bool _isCompleted;
    private string? _validationMessage;
    private string? _errorMessage;
    private string _weightUnitCode;
    private string _circumferenceUnitCode;
    private DateTime _measurementDate;
    private EntitlementSnapshot _entitlement = new(
        EntitlementTier.Free,
        SubscriptionState.Active,
        null,
        null,
        null);
    private bool _entitlementsLoaded;

    public MeasurementEditorViewModel(
        RecordMeasurement recordMeasurement,
        CalculateBodyFat calculateBodyFat,
        CalculateBasalMetabolicRate calculateBmr,
        CalculateTotalDailyEnergyExpenditure calculateTdee,
        ProfileDto profile,
        MeasurementType measurementType,
        IMeasurementNavigation navigation,
        LanguageService languageService,
        DisplayPreferencesService displayPreferences,
        UpdateMeasurement? updateMeasurement = null,
        MeasurementDto? existingMeasurement = null,
        IEntitlementProvider? entitlementProvider = null,
        IClock? clock = null)
    {
        _recordMeasurement = recordMeasurement;
        _updateMeasurement = updateMeasurement;
        _calculateBodyFat = calculateBodyFat;
        _calculateBmr = calculateBmr;
        _calculateTdee = calculateTdee;
        _profile = profile;
        _measurementType = measurementType;
        _navigation = navigation;
        _languageService = languageService;
        _displayPreferences = displayPreferences;
        _existingMeasurement = existingMeasurement;
        _entitlementProvider = entitlementProvider ?? FreeEntitlementProvider.Instance;
        _clock = clock;
        _entitledDisplayPreferences = new EntitledDisplayPreferences(_displayPreferences, _entitlement);
        _weightUnitCode = _entitledDisplayPreferences.WeightUnitCode;
        _circumferenceUnitCode = _entitledDisplayPreferences.CircumferenceUnitCode;
        _measurementDate = (_existingMeasurement?.MeasuredAtUtc ?? NowUtc).ToLocalTime().Date;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        CancelCommand = new AsyncRelayCommand(_navigation.CancelAsync);
        ShowGuidanceCommand = new AsyncRelayCommand<GuidanceTopic>(_navigation.ShowGuidanceAsync);
        ShowPremiumCommand = new AsyncRelayCommand(_navigation.ShowPremiumAsync);
        _displayPreferences.PreferencesChanged += OnDisplayPreferencesChanged;
        LoadExistingMeasurement();
    }

    public string Title => _existingMeasurement is not null
        ? _languageService.Get("EditMeasurementTitle")
        : _measurementType == MeasurementType.WeightOnly
            ? _languageService.Get("AddWeight")
            : _languageService.Get("AddMeasurements");

    public string SaveButtonText => _measurementType == MeasurementType.WeightOnly
        ? _languageService.Get("SaveWeight")
        : _languageService.Get("CalculateResults");

    public bool IsExtended => _measurementType == MeasurementType.WeightAndSizes;

    public DateTime MeasurementDate
    {
        get => _measurementDate;
        set
        {
            if (SetProperty(ref _measurementDate, value.Date))
            {
                OnPropertyChanged(nameof(CanSave));
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public DateTime MaximumMeasurementDate => NowUtc.ToLocalTime().Date;

    public bool IsMeasurementDateEnabled
        => FeatureAccessPolicy.CanUse(_entitlement, PremiumFeature.PastMeasurements);

    public bool IsMeasurementDateLocked => !IsMeasurementDateEnabled;

    public string MeasurementDatePremiumText => _languageService.Get("PremiumRequired");

    public bool IsFemale => _profile.Gender == ProfileGender.Female;

    public string TrunkLabel => _languageService.Get(IsFemale ? "Waist" : "Abdomen");

    public GuidanceTopic TrunkGuidanceTopic => IsFemale ? GuidanceTopic.Waist : GuidanceTopic.Abdomen;

    public string WeightUnitText => _languageService.Get(
        _entitledDisplayPreferences.WeightUnitCode == DisplayPreferencesService.PoundsCode ? "Lb" : "Kg");

    public string LengthUnitText => _languageService.Get(
        _entitledDisplayPreferences.CircumferenceUnitCode == DisplayPreferencesService.InchesCode ? "In" : "Cm");

    public string WeightText
    {
        get => _weightText;
        set => SetInput(ref _weightText, value);
    }

    public string NeckText
    {
        get => _neckText;
        set => SetInput(ref _neckText, value);
    }

    public string AbdomenText
    {
        get => _abdomenText;
        set => SetInput(ref _abdomenText, value);
    }

    public string HipText
    {
        get => _hipText;
        set => SetInput(ref _hipText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSave));
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        private set => SetProperty(ref _isCompleted, value);
    }

    public bool CanSave => !IsBusy && HasValidInput();

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand CancelCommand { get; }

    public IAsyncRelayCommand<GuidanceTopic> ShowGuidanceCommand { get; }

    public IAsyncRelayCommand ShowPremiumCommand { get; }

    public async Task LoadEntitlementsAsync()
    {
        if (_entitlementsLoaded)
        {
            return;
        }

        try
        {
            _entitlement = await _entitlementProvider.GetCurrentAsync(CancellationToken.None);
            _entitledDisplayPreferences.SetEntitlement(_entitlement);
            OnDisplayPreferencesChanged(this, EventArgs.Empty);
        }
        catch
        {
            _entitlement = new EntitlementSnapshot(
                EntitlementTier.Free,
                SubscriptionState.Active,
                null,
                null,
                null);
        }
        finally
        {
            _entitlementsLoaded = true;
            OnPropertyChanged(nameof(IsMeasurementDateEnabled));
            OnPropertyChanged(nameof(IsMeasurementDateLocked));
            OnPropertyChanged(nameof(CanSave));
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task SaveAsync()
    {
        ValidationMessage = null;
        ErrorMessage = null;
        if (!CanSave || !TryCreateCommand(out var command))
        {
            ValidationMessage = IsExtended
                ? _languageService.Get(IsFemale ? "ValidMeasurementExtendedFemale" : "ValidMeasurementExtended")
                : _languageService.Get("ValidWeight");
            return;
        }

        IsBusy = true;
        try
        {
            var recorded = _existingMeasurement is null
                ? await _recordMeasurement.ExecuteAsync(command, CancellationToken.None)
                : await UpdateExistingMeasurementAsync(command);
            if (!recorded.IsSuccess)
            {
                if (recorded.Error!.Code is "measurement.pastDate.premiumRequired" or "measurement.date.invalid")
                {
                    ValidationMessage = _languageService.Get("MeasurementDateError");
                }
                ValidationMessage = recorded.Error!.Code.StartsWith("measurement.", StringComparison.Ordinal)
                    && recorded.Error!.Code is not "measurement.pastDate.premiumRequired" and not "measurement.date.invalid"
                    ? _languageService.Get("MeasurementValuesError")
                    : null;
                ErrorMessage = recorded.Error!.Code == "profile.settings.required"
                    ? _languageService.Get("CompleteProfileDetails")
                    : ValidationMessage is null ? _languageService.Get("SaveMeasurementError") : null;
                return;
            }

            var bodyFat = await _calculateBodyFat.ExecuteAsync(new CalculateBodyFatCommand(_profile.Id, recorded.Value.Id), CancellationToken.None);
            var bmr = await _calculateBmr.ExecuteAsync(new CalculateBmrCommand(_profile.Id, recorded.Value.Id), CancellationToken.None);
            var tdee = await _calculateTdee.ExecuteAsync(new CalculateTdeeCommand(_profile.Id, recorded.Value.Id), CancellationToken.None);
            if (!bodyFat.IsSuccess || !bmr.IsSuccess || !tdee.IsSuccess)
            {
                ErrorMessage = _languageService.Get("CalculationError");
                return;
            }

            IsCompleted = true;
            await _navigation.ShowResultsAsync(_profile, recorded.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryCreateCommand(out RecordMeasurementCommand command)
    {
        if (!TryParseDecimal(WeightText, out var enteredWeight))
        {
            command = null!;
            return false;
        }

        var weight = _entitledDisplayPreferences.ToMetricWeight(enteredWeight);
        if (weight is < 1m or > 500m)
        {
            command = null!;
            return false;
        }

        decimal? neck = null;
        decimal? abdomen = null;
        decimal? hip = null;
        if (IsExtended)
        {
            if (!TryParseDecimal(NeckText, out var neckValue)
                || !TryParseDecimal(AbdomenText, out var abdomenValue))
            {
                command = null!;
                return false;
            }

            neck = _entitledDisplayPreferences.ToMetricCircumference(neckValue);
            abdomen = _entitledDisplayPreferences.ToMetricCircumference(abdomenValue);
            if (neck is < 1m or > 100m || abdomen is < 1m or > 400m)
            {
                command = null!;
                return false;
            }

            if (IsFemale)
            {
                if (!TryParseDecimal(HipText, out var hipValue))
                {
                    command = null!;
                    return false;
                }

                hip = _entitledDisplayPreferences.ToMetricCircumference(hipValue);
                if (hip is < 1m or > 400m)
                {
                    command = null!;
                    return false;
                }
            }
        }

        command = new RecordMeasurementCommand(
            _profile.Id,
            _measurementType,
            weight,
            neck,
            abdomen,
            _existingMeasurement is null ? NewMeasurementTimestampUtc() : _existingMeasurement.MeasuredAtUtc,
            hip);
        return true;
    }

    private Task<Result<MeasurementDto>> UpdateExistingMeasurementAsync(RecordMeasurementCommand command)
    {
        if (_updateMeasurement is null || _profile.Settings is null || _existingMeasurement is null)
        {
            return Task.FromResult(Result.Failure<MeasurementDto>(new DomainError(
                "measurement.update.unavailable",
                "Errors.PersistenceUnavailable")));
        }

        var measuredAtUtc = _existingMeasurement.MeasuredAtUtc;
        if (IsMeasurementDateEnabled
            && MeasurementDate != _existingMeasurement.MeasuredAtUtc.ToLocalTime().Date)
        {
            measuredAtUtc = ToUtcAtLocalNoon(MeasurementDate);
        }

        return _updateMeasurement.ExecuteAsync(
            new UpdateMeasurementCommand(
                _existingMeasurement.Id,
                command.ProfileId,
                command.Type,
                command.WeightKg,
                _profile.Settings.HeightCm,
                command.NeckCm,
                command.AbdomenCm,
                _profile.Settings.AgeYears,
                _profile.Settings.ActivityLevel,
                measuredAtUtc,
                command.HipCm,
                _profile.Gender),
            CancellationToken.None);
    }

    private bool HasValidInput()
        => TryCreateCommand(out _);

    private DateTimeOffset NowUtc => _clock?.UtcNow ?? DateTimeOffset.UtcNow;

    private DateTimeOffset NewMeasurementTimestampUtc()
        => ToUtcAtLocalNoon(IsMeasurementDateEnabled ? MeasurementDate : NowUtc.ToLocalTime().Date);

    private static DateTimeOffset ToUtcAtLocalNoon(DateTime localDate)
    {
        var localNoon = localDate.Date.AddHours(12);
        var offset = TimeZoneInfo.Local.GetUtcOffset(localNoon);
        return new DateTimeOffset(localNoon, offset).ToUniversalTime();
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        var normalized = value.Trim().Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result);
    }

    private void SetInput(ref string field, string value)
    {
        if (SetProperty(ref field, value))
        {
            OnPropertyChanged(nameof(CanSave));
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private void LoadExistingMeasurement()
    {
        if (_existingMeasurement is null)
        {
            return;
        }

        _weightText = _entitledDisplayPreferences.ToDisplayWeight(_existingMeasurement.WeightKg).ToString("0.##", CultureInfo.CurrentCulture);
        _neckText = _existingMeasurement.NeckCm.HasValue
            ? _entitledDisplayPreferences.ToDisplayCircumference(_existingMeasurement.NeckCm.Value).ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;
        _abdomenText = _existingMeasurement.AbdomenCm.HasValue
            ? _entitledDisplayPreferences.ToDisplayCircumference(_existingMeasurement.AbdomenCm.Value).ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;
        _hipText = _existingMeasurement.HipCm.HasValue
            ? _entitledDisplayPreferences.ToDisplayCircumference(_existingMeasurement.HipCm.Value).ToString("0.##", CultureInfo.CurrentCulture)
            : string.Empty;
        OnPropertyChanged(nameof(WeightText));
        OnPropertyChanged(nameof(NeckText));
        OnPropertyChanged(nameof(AbdomenText));
        OnPropertyChanged(nameof(HipText));
        OnPropertyChanged(nameof(MeasurementDate));
    }

    private void OnDisplayPreferencesChanged(object? sender, EventArgs e)
    {
        if (TryParseDecimal(_weightText, out var enteredWeight))
        {
            var weightKg = DisplayPreferencesService.ConvertWeightToMetric(enteredWeight, _weightUnitCode);
            _weightText = DisplayPreferencesService.ConvertWeightToDisplay(weightKg, _entitledDisplayPreferences.WeightUnitCode).ToString("0.##", CultureInfo.CurrentCulture);
            OnPropertyChanged(nameof(WeightText));
        }

        if (TryParseDecimal(_neckText, out var enteredNeck))
        {
            var neckCm = DisplayPreferencesService.ConvertCircumferenceToMetric(enteredNeck, _circumferenceUnitCode);
            _neckText = DisplayPreferencesService.ConvertCircumferenceToDisplay(
                neckCm,
                _entitledDisplayPreferences.CircumferenceUnitCode).ToString("0.##", CultureInfo.CurrentCulture);
            OnPropertyChanged(nameof(NeckText));
        }

        if (TryParseDecimal(_abdomenText, out var enteredAbdomen))
        {
            var abdomenCm = DisplayPreferencesService.ConvertCircumferenceToMetric(enteredAbdomen, _circumferenceUnitCode);
            _abdomenText = DisplayPreferencesService.ConvertCircumferenceToDisplay(
                abdomenCm,
                _entitledDisplayPreferences.CircumferenceUnitCode).ToString("0.##", CultureInfo.CurrentCulture);
            OnPropertyChanged(nameof(AbdomenText));
        }

        if (TryParseDecimal(_hipText, out var enteredHip))
        {
            var hipCm = DisplayPreferencesService.ConvertCircumferenceToMetric(enteredHip, _circumferenceUnitCode);
            _hipText = DisplayPreferencesService.ConvertCircumferenceToDisplay(
                hipCm,
                _entitledDisplayPreferences.CircumferenceUnitCode).ToString("0.##", CultureInfo.CurrentCulture);
            OnPropertyChanged(nameof(HipText));
        }

        _weightUnitCode = _entitledDisplayPreferences.WeightUnitCode;
        _circumferenceUnitCode = _entitledDisplayPreferences.CircumferenceUnitCode;
        OnPropertyChanged(nameof(WeightUnitText));
        OnPropertyChanged(nameof(LengthUnitText));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }
}

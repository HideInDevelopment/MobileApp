using System.Globalization;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Measurements;

public sealed class MeasurementEditorViewModel : ObservableObject
{
    private readonly RecordMeasurement _recordMeasurement;
    private readonly CalculateBodyFat _calculateBodyFat;
    private readonly CalculateBasalMetabolicRate _calculateBmr;
    private readonly CalculateTotalDailyEnergyExpenditure _calculateTdee;
    private readonly ProfileDto _profile;
    private readonly MeasurementType _measurementType;
    private readonly IMeasurementNavigation _navigation;
    private readonly LanguageService _languageService;
    private string _weightText = string.Empty;
    private string _neckText = string.Empty;
    private string _abdomenText = string.Empty;
    private string _hipText = string.Empty;
    private bool _isBusy;
    private bool _isCompleted;
    private string? _validationMessage;
    private string? _errorMessage;

    public MeasurementEditorViewModel(
        RecordMeasurement recordMeasurement,
        CalculateBodyFat calculateBodyFat,
        CalculateBasalMetabolicRate calculateBmr,
        CalculateTotalDailyEnergyExpenditure calculateTdee,
        ProfileDto profile,
        MeasurementType measurementType,
        IMeasurementNavigation navigation,
        LanguageService languageService)
    {
        _recordMeasurement = recordMeasurement;
        _calculateBodyFat = calculateBodyFat;
        _calculateBmr = calculateBmr;
        _calculateTdee = calculateTdee;
        _profile = profile;
        _measurementType = measurementType;
        _navigation = navigation;
        _languageService = languageService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        CancelCommand = new AsyncRelayCommand(_navigation.CancelAsync);
    }

    public string Title => _measurementType == MeasurementType.WeightOnly
        ? _languageService.Get("AddWeight")
        : _languageService.Get("AddMeasurements");

    public string SaveButtonText => _measurementType == MeasurementType.WeightOnly
        ? _languageService.Get("SaveWeight")
        : _languageService.Get("CalculateResults");

    public bool IsExtended => _measurementType == MeasurementType.WeightAndSizes;

    public bool IsFemale => _profile.Gender == ProfileGender.Female;

    public string TrunkLabel => _languageService.Get(IsFemale ? "Waist" : "Abdomen");

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

    public bool CanSave => !IsBusy
        && TryParseDecimal(WeightText, out var weight) && weight is >= 1m and <= 500m
        && (!IsExtended ||
            (TryParseDecimal(NeckText, out var neck) && neck is >= 1m and <= 100m
            && TryParseDecimal(AbdomenText, out var abdomen) && abdomen is >= 1m and <= 400m
            && (!IsFemale || TryParseDecimal(HipText, out var hip) && hip is >= 1m and <= 400m)));

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
            var recorded = await _recordMeasurement.ExecuteAsync(command, CancellationToken.None);
            if (!recorded.IsSuccess)
            {
                ValidationMessage = recorded.Error!.Code.StartsWith("measurement.", StringComparison.Ordinal)
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
            await _navigation.CloseMeasurementAsync();
            await _navigation.ShowHistoryAsync(_profile);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryCreateCommand(out RecordMeasurementCommand command)
    {
        if (!TryParseDecimal(WeightText, out var weight))
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

            neck = neckValue;
            abdomen = abdomenValue;
            if (IsFemale)
            {
                if (!TryParseDecimal(HipText, out var hipValue))
                {
                    command = null!;
                    return false;
                }

                hip = hipValue;
            }
        }

        command = new RecordMeasurementCommand(_profile.Id, _measurementType, weight, neck, abdomen, DateTimeOffset.UtcNow, hip);
        return true;
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
}

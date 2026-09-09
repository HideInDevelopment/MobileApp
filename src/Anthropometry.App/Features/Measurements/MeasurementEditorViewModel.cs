using System.Globalization;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Measurements;
using Anthropometry.Domain.Calculations;
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
    private readonly ProfileId _profileId;
    private readonly IMeasurementNavigation _navigation;
    private string _weightText = string.Empty;
    private string _heightText = string.Empty;
    private string _neckText = string.Empty;
    private string _abdomenText = string.Empty;
    private string _ageText = string.Empty;
    private ActivityLevel _selectedActivityLevel;
    private bool _isBusy;
    private bool _isCompleted;
    private string? _validationMessage;
    private string? _errorMessage;

    public MeasurementEditorViewModel(
        RecordMeasurement recordMeasurement,
        CalculateBodyFat calculateBodyFat,
        CalculateBasalMetabolicRate calculateBmr,
        CalculateTotalDailyEnergyExpenditure calculateTdee,
        ProfileId profileId,
        IMeasurementNavigation navigation)
    {
        _recordMeasurement = recordMeasurement;
        _calculateBodyFat = calculateBodyFat;
        _calculateBmr = calculateBmr;
        _calculateTdee = calculateTdee;
        _profileId = profileId;
        _navigation = navigation;
        ActivityLevels = Enum.GetValues<ActivityLevel>()
            .Where(level => level != ActivityLevel.Unknown)
            .ToArray();
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => CanSave);
        CancelCommand = new AsyncRelayCommand(_navigation.CancelAsync);
    }

    public IReadOnlyList<ActivityLevel> ActivityLevels { get; }

    public string WeightText
    {
        get => _weightText;
        set => SetInput(ref _weightText, value);
    }

    public string HeightText
    {
        get => _heightText;
        set => SetInput(ref _heightText, value);
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

    public string AgeText
    {
        get => _ageText;
        set => SetInput(ref _ageText, value);
    }

    public ActivityLevel SelectedActivityLevel
    {
        get => _selectedActivityLevel;
        set
        {
            if (SetProperty(ref _selectedActivityLevel, value))
            {
                OnPropertyChanged(nameof(CanSave));
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
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
        && TryParseDecimal(HeightText, out var height) && height is >= 50m and <= 300m
        && TryParseDecimal(NeckText, out var neck) && neck is >= 1m and <= 100m
        && TryParseDecimal(AbdomenText, out var abdomen) && abdomen is >= 1m and <= 400m
        && int.TryParse(AgeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var age) && age is >= 1 and <= 120
        && SelectedActivityLevel != ActivityLevel.Unknown;

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
        if (!CanSave || !TryCreateInput(out var input))
        {
            ValidationMessage = "Enter valid metric values for all fields and choose an activity level.";
            return;
        }

        IsBusy = true;
        try
        {
            var recorded = await _recordMeasurement.ExecuteAsync(
                new RecordMeasurementCommand(
                    _profileId,
                    MeasurementType.WeightAndSizes,
                    input.WeightKg,
                    input.NeckCm,
                    input.AbdomenCm,
                    input.MeasuredAtUtc),
                CancellationToken.None);
            if (!recorded.IsSuccess)
            {
                ValidationMessage = recorded.Error!.Code.StartsWith("measurement.", StringComparison.Ordinal)
                    ? "Check the measurement values and try again."
                    : null;
                ErrorMessage = ValidationMessage is null ? "We couldn't save this measurement. Try again." : null;
                return;
            }

            var bodyFat = await _calculateBodyFat.ExecuteAsync(new CalculateBodyFatCommand(_profileId, recorded.Value.Id), CancellationToken.None);
            var bmr = await _calculateBmr.ExecuteAsync(new CalculateBmrCommand(_profileId, recorded.Value.Id), CancellationToken.None);
            var tdee = await _calculateTdee.ExecuteAsync(new CalculateTdeeCommand(_profileId, recorded.Value.Id), CancellationToken.None);
            if (!bodyFat.IsSuccess || !bmr.IsSuccess || !tdee.IsSuccess)
            {
                ErrorMessage = "We couldn't calculate results. Try again.";
                return;
            }

            IsCompleted = true;
            await _navigation.ShowResultsAsync(recorded.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryCreateInput(out MeasurementInput input)
    {
        if (!TryParseDecimal(WeightText, out var weight)
            || !TryParseDecimal(HeightText, out var height)
            || !TryParseDecimal(NeckText, out var neck)
            || !TryParseDecimal(AbdomenText, out var abdomen)
            || !int.TryParse(AgeText, NumberStyles.Integer, CultureInfo.CurrentCulture, out var age))
        {
            input = null!;
            return false;
        }

        input = new MeasurementInput(MeasurementType.WeightAndSizes, weight, height, neck, abdomen, age, SelectedActivityLevel, DateTimeOffset.UtcNow);
        return true;
    }

    private static bool TryParseDecimal(string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private void SetInput(ref string field, string value)
    {
        if (SetProperty(ref field, value))
        {
            OnPropertyChanged(nameof(CanSave));
            SaveCommand.NotifyCanExecuteChanged();
        }
    }
}

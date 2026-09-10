using System.Collections.ObjectModel;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.App.Localization;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Results;

public sealed record CalculationResultDisplayItem(string Title, string Value, string Unit);

public sealed class CalculationResultViewModel : ObservableObject
{
    private readonly GetCalculationResults _getResults;
    private readonly MeasurementId _measurementId;
    private readonly MeasurementType _measurementType;
    private readonly LanguageService _languageService;
    private readonly ObservableCollection<CalculationResultDisplayItem> _results = [];
    private bool _isLoading;
    private string? _errorMessage;

    public CalculationResultViewModel(
        GetCalculationResults getResults,
        MeasurementId measurementId,
        MeasurementType measurementType,
        LanguageService languageService)
    {
        _getResults = getResults;
        _measurementId = measurementId;
        _measurementType = measurementType;
        _languageService = languageService;
        Results = new ReadOnlyObservableCollection<CalculationResultDisplayItem>(_results);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ReadOnlyObservableCollection<CalculationResultDisplayItem> Results { get; }

    public bool ShowWarningIcon => _measurementType == MeasurementType.WeightOnly;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsEmpty => !IsLoading && Results.Count == 0 && ErrorMessage is null;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public IAsyncRelayCommand LoadCommand { get; }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _getResults.ExecuteAsync(_measurementId, CancellationToken.None);
            _results.Clear();
            if (result.IsSuccess)
            {
                foreach (var item in result.Value)
                {
                    _results.Add(ToDisplayItem(item));
                }
            }
            else
            {
                ErrorMessage = _languageService.Get("ResultsLoadError");
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private CalculationResultDisplayItem ToDisplayItem(CalculationResultDto result)
        => new(
            result.CalculationType switch
            {
                CalculationType.BodyFatPercentage => _languageService.Get("BodyFatPercentage"),
                CalculationType.BasalMetabolicRate => _languageService.Get("BasalMetabolicRate"),
                CalculationType.TotalDailyEnergyExpenditure => _languageService.Get("TotalDailyEnergyExpenditure"),
                _ => result.CalculationType.ToString()
            },
            result.Value.ToString("F2", System.Globalization.CultureInfo.CurrentCulture),
            result.Unit);
}

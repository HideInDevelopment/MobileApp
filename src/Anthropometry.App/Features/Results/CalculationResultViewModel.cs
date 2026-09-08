using System.Collections.ObjectModel;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
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
    private readonly ObservableCollection<CalculationResultDisplayItem> _results = [];
    private bool _isLoading;
    private string? _errorMessage;

    public CalculationResultViewModel(GetCalculationResults getResults, MeasurementId measurementId)
    {
        _getResults = getResults;
        _measurementId = measurementId;
        Results = new ReadOnlyObservableCollection<CalculationResultDisplayItem>(_results);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ReadOnlyObservableCollection<CalculationResultDisplayItem> Results { get; }

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
                ErrorMessage = "We couldn't load results. Try again.";
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private static CalculationResultDisplayItem ToDisplayItem(CalculationResultDto result)
        => new(
            result.CalculationType switch
            {
                CalculationType.BodyFatPercentage => "Body Fat Percentage",
                CalculationType.BasalMetabolicRate => "Basal Metabolic Rate",
                CalculationType.TotalDailyEnergyExpenditure => "Total Daily Energy Expenditure",
                _ => result.CalculationType.ToString()
            },
            result.Value.ToString("F2", System.Globalization.CultureInfo.CurrentCulture),
            result.Unit);
}

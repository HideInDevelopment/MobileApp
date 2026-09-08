using System.Collections.ObjectModel;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Measurements;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Results;

public sealed class CalculationResultViewModel : ObservableObject
{
    private readonly GetCalculationResults _getResults;
    private readonly MeasurementId _measurementId;
    private readonly ObservableCollection<CalculationResultDto> _results = [];
    private bool _isLoading;
    private string? _errorMessage;

    public CalculationResultViewModel(GetCalculationResults getResults, MeasurementId measurementId)
    {
        _getResults = getResults;
        _measurementId = measurementId;
        Results = new ReadOnlyObservableCollection<CalculationResultDto>(_results);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public ReadOnlyObservableCollection<CalculationResultDto> Results { get; }

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

    public string FormulaDetails => string.Join(
        Environment.NewLine,
        Results.Select(result => $"{result.FormulaId} v{result.FormulaVersion}"));

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
                    _results.Add(item);
                }
                OnPropertyChanged(nameof(FormulaDetails));
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
}

using Anthropometry.App.Platforms.Android;

namespace Anthropometry.App.Features.Settings;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnLanguageSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is SettingsViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.Languages
                    .Select(option => new ContextMenuOption(
                        viewModel.GetLanguageDisplayName(option),
                        () => viewModel.SelectLanguageCommand.Execute(option.Code)))
                    .ToArray());
        }
    }

    private void OnThemeSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is SettingsViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.Themes
                    .Select(option => new ContextMenuOption(
                        option.DisplayName,
                        () => viewModel.SelectThemeCommand.Execute(option.Code)))
                    .ToArray());
        }
    }

    private void OnDateFormatSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is SettingsViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.DateFormats
                    .Select(option => new ContextMenuOption(
                        option.DisplayName,
                        () => viewModel.SelectDateFormatCommand.Execute(option.Code)))
                    .ToArray());
        }
    }

    private void OnMeasurementSystemSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is SettingsViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.MeasurementSystems
                    .Select(option => new ContextMenuOption(
                        option.DisplayName,
                        () => viewModel.SelectMeasurementSystemCommand.Execute(option.Code)))
                    .ToArray());
        }
    }

    private void OnInactivityIntervalSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is SettingsViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.InactivityIntervals
                    .Select(option => new ContextMenuOption(
                        option.DisplayName,
                        () => viewModel.SelectInactivityIntervalCommand.Execute(option.Days.ToString(System.Globalization.CultureInfo.InvariantCulture))))
                    .ToArray());
        }
    }

    private async void OnDailyReminderToggled(object? sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel)
        {
            await viewModel.SetDailyReminderEnabledAsync(e.Value);
        }
    }

    private async void OnInactivityReminderToggled(object? sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel)
        {
            await viewModel.SetInactivityReminderEnabledAsync(e.Value);
        }
    }
}

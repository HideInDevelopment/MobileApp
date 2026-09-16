namespace Anthropometry.App.Features.Settings;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
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

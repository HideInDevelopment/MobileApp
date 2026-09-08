using Anthropometry.Infrastructure.Persistence.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Anthropometry.App;

public partial class App : Application
{
    private readonly MigrationRunner _migrationRunner;
    private readonly IServiceProvider _services;

    public App(MigrationRunner migrationRunner, IServiceProvider services)
    {
        InitializeComponent();
        _migrationRunner = migrationRunner;
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(CreateLoadingPage());
        _ = InitializeAsync(window);
        return window;
    }

    private async Task InitializeAsync(Window window)
    {
        try
        {
            await _migrationRunner.InitializeAsync(CancellationToken.None);
            window.Page = _services.GetRequiredService<AppShell>();
        }
        catch (Exception)
        {
            var retry = new Button { Text = "Retry", MinimumHeightRequest = 48 };
            retry.Clicked += async (_, _) =>
            {
                retry.IsEnabled = false;
                await InitializeAsync(window);
                retry.IsEnabled = true;
            };
            window.Page = new ContentPage
            {
                Title = "Database unavailable",
                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    Spacing = 16,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = "We couldn't open local storage. Try again.", HorizontalTextAlignment = TextAlignment.Center },
                        retry
                    }
                }
            };
        }
    }

    private static Page CreateLoadingPage()
        => new ContentPage
        {
            Title = "Anthropometry",
            Content = new ActivityIndicator { IsRunning = true, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
}

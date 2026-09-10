using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.App.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Anthropometry.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly MigrationRunner _migrationRunner;
    private readonly IServiceProvider _services;
    private readonly LanguageService _languageService;

    public App(MigrationRunner migrationRunner, IServiceProvider services, LanguageService languageService)
    {
        InitializeComponent();
        _migrationRunner = migrationRunner;
        _services = services;
        _languageService = languageService;
        _languageService.LanguageChanged += (_, _) => ApplyLocalizedResources();
        _languageService.Initialize();
        ApplyLocalizedResources();
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
            var retry = new Button { Text = _languageService.Get("Retry"), MinimumHeightRequest = 48 };
            retry.Clicked += async (_, _) =>
            {
                retry.IsEnabled = false;
                await InitializeAsync(window);
                retry.IsEnabled = true;
            };
            window.Page = new ContentPage
            {
                Title = _languageService.Get("DatabaseUnavailableTitle"),
                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    Spacing = 16,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = _languageService.Get("DatabaseUnavailableMessage"), HorizontalTextAlignment = TextAlignment.Center },
                        retry
                    }
                }
            };
        }
    }

    private ContentPage CreateLoadingPage()
        => new ContentPage
        {
            Title = _languageService.Get("AppTitle"),
            Content = new ActivityIndicator { IsRunning = true, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

    private void ApplyLocalizedResources()
    {
        foreach (var key in LanguageService.ResourceKeys)
        {
            Resources[key] = _languageService.Get(key);
        }
    }
}

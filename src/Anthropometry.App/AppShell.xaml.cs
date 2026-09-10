using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Localization;

namespace Anthropometry.App;

public partial class AppShell : Shell
{
    public AppShell(ProfileListPage profileListPage, LanguageService languageService)
    {
        InitializeComponent();
        var profiles = new ShellContent
        {
            Title = languageService.Get("ProfilesTitle"),
            Route = "profiles",
            Content = profileListPage
        };
        Items.Add(profiles);
        languageService.LanguageChanged += (_, _) =>
        {
            Title = languageService.Get("AppTitle");
            profiles.Title = languageService.Get("ProfilesTitle");
        };
    }
}

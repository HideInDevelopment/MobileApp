using Anthropometry.App.Features.Profiles;

namespace Anthropometry.App;

public partial class AppShell : Shell
{
    public AppShell(ProfileListPage profileListPage)
    {
        InitializeComponent();
        Items.Add(new ShellContent
        {
            Title = "Profiles",
            Route = "profiles",
            Content = profileListPage
        });
    }
}

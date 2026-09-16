using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Anthropometry.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnResume()
    {
        base.OnResume();
        Microsoft.Maui.IPlatformApplication.Current?.Services
            .GetService<Anthropometry.App.Features.Settings.ReminderCoordinator>()
            ?.MarkAppOpened(DateTimeOffset.UtcNow);
    }
}

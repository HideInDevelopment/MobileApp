using Android;
using Microsoft.Maui.ApplicationModel;

namespace Anthropometry.App;

public sealed class AndroidNotificationPermissionRequest : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        => OperatingSystem.IsAndroidVersionAtLeast(33)
            ? [(Manifest.Permission.PostNotifications, true)]
            : [];
}

public sealed class AndroidReminderPermissionService : Features.Settings.IReminderPermissionService
{
    public async Task<bool> EnsureGrantedAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return true;
        }

        var status = await Permissions.CheckStatusAsync<AndroidNotificationPermissionRequest>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<AndroidNotificationPermissionRequest>();
        }

        return status == PermissionStatus.Granted;
    }
}

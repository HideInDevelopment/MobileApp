using Android.Widget;

namespace Anthropometry.App.Platforms.Android;

public sealed record ContextMenuOption(string Text, Action Action);

public static class ContextMenuHelper
{
    public static void Show(Microsoft.Maui.Controls.View anchor, params ContextMenuOption[] options)
    {
        if (anchor.Handler?.PlatformView is not global::Android.Views.View nativeAnchor
            || anchor.Handler.MauiContext?.Context is not { } context)
        {
            return;
        }

        var popup = new PopupMenu(context, nativeAnchor);
        var menu = popup.Menu;
        if (menu is null)
        {
            return;
        }

        for (var index = 0; index < options.Length; index++)
        {
            menu.Add(0, index, index, new Java.Lang.String(options[index].Text));
        }

        popup.MenuItemClick += (_, args) =>
        {
            if (args.Item?.ItemId is >= 0 and var itemId && itemId < options.Length)
            {
                options[itemId].Action();
            }
        };
        popup.Show();
    }
}

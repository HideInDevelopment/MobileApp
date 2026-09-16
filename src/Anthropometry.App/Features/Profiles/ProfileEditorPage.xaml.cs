using Anthropometry.App.Platforms.Android;

namespace Anthropometry.App.Features.Profiles;

public partial class ProfileEditorPage : ContentPage
{
    public ProfileEditorPage(ProfileEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void OnGenderSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is ProfileEditorViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.GenderOptions
                    .Select(option => new ContextMenuOption(
                        option.DisplayName,
                        () => viewModel.SelectGenderCommand.Execute(option.Value.ToString())))
                    .ToArray());
        }
    }

    private void OnActivityLevelSelectorClicked(object? sender, EventArgs e)
    {
        if (sender is View view && BindingContext is ProfileEditorViewModel viewModel)
        {
            ContextMenuHelper.Show(
                view,
                viewModel.ActivityLevels
                    .Select(option => new ContextMenuOption(
                        option.DisplayName,
                        () => viewModel.SelectActivityLevelCommand.Execute(option.Value.ToString())))
                    .ToArray());
        }
    }
}

namespace Anthropometry.App.Features.Profiles;

public partial class ProfileEditorPage : ContentPage
{
    public ProfileEditorPage(ProfileEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

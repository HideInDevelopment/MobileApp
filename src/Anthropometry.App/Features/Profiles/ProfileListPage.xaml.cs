namespace Anthropometry.App.Features.Profiles;

public partial class ProfileListPage : ContentPage
{
    private readonly ProfileListViewModel _viewModel;
    public ProfileListPage(ProfileListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}

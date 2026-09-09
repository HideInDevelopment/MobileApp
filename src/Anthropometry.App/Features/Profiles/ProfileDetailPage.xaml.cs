using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;

namespace Anthropometry.App.Features.Profiles;

public partial class ProfileDetailPage : ContentPage
{
    public ProfileDetailPage(ProfileDto profile, GetMeasurementHistory getHistory, IProfileNavigation navigation)
    {
        InitializeComponent();
        BindingContext = new ProfileDetailViewModel(profile, getHistory, navigation);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProfileDetailViewModel viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}

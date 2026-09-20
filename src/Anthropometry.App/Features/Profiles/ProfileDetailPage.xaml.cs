using Anthropometry.Application.Common;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Abstractions;
using Anthropometry.App.Localization;

namespace Anthropometry.App.Features.Profiles;

public partial class ProfileDetailPage : ContentPage
{
    public ProfileDetailPage(
        ProfileDto profile,
        GetMeasurementHistory getHistory,
        IProfileNavigation navigation,
        LanguageService languageService,
        IEntitlementProvider? entitlementProvider = null)
    {
        InitializeComponent();
        BindingContext = new ProfileDetailViewModel(profile, getHistory, navigation, languageService, entitlementProvider);
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

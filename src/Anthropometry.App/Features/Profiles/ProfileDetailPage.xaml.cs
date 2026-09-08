using Anthropometry.Application.Common;

namespace Anthropometry.App.Features.Profiles;

public partial class ProfileDetailPage : ContentPage
{
    public ProfileDetailPage(ProfileDto profile, IProfileNavigation navigation)
    {
        InitializeComponent();
        BindingContext = new ProfileDetailViewModel(profile, navigation);
    }
}

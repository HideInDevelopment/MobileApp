using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Entitlements;
using Anthropometry.App.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Features.Premium;

public sealed class PremiumViewModel : ObservableObject
{
    private readonly EntitlementService _entitlementService;
    private readonly IBillingGateway _billing;
    private readonly LanguageService _languageService;
    private EntitlementSnapshot _entitlement;
    private string? _errorMessage;
    private bool _isBusy;

    public PremiumViewModel(
        EntitlementService entitlementService,
        IBillingGateway billing,
        LanguageService languageService)
    {
        _entitlementService = entitlementService;
        _billing = billing;
        _languageService = languageService;
        _entitlement = new EntitlementSnapshot(
            EntitlementTier.Free,
            SubscriptionState.Active,
            null,
            null,
            null);
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SubscribeCommand = new AsyncRelayCommand(SubscribeAsync, () => !IsBusy);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync, () => !IsBusy);
        ManageSubscriptionCommand = new AsyncRelayCommand(ManageSubscriptionAsync, () => IsPremium && !IsBusy);
        _languageService.LanguageChanged += OnLanguageChanged;
    }

    public EntitlementTier Tier => _entitlement.Tier;

    public SubscriptionState State => _entitlement.State;

    public bool IsPremium
        => FeatureAccessPolicy.CanUse(_entitlement, PremiumFeature.ImperialUnits);

    public string StateText
        => _languageService.Get(IsPremium ? "PremiumStatusActive" : "PremiumStatusFree");

    public string Description => _languageService.Get("PremiumDescription");

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            SubscribeCommand.NotifyCanExecuteChanged();
            RestoreCommand.NotifyCanExecuteChanged();
            ManageSubscriptionCommand.NotifyCanExecuteChanged();
        }
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SubscribeCommand { get; }

    public IAsyncRelayCommand RestoreCommand { get; }

    public IAsyncRelayCommand ManageSubscriptionCommand { get; }

    private async Task LoadAsync()
    {
        await RunBillingOperationAsync(async () =>
        {
            var snapshot = await _entitlementService.GetCurrentAsync(CancellationToken.None);
            SetEntitlement(snapshot);
        }, showError: false);
    }

    private async Task SubscribeAsync()
    {
        await RunBillingOperationAsync(async () =>
        {
            var catalog = await _billing.GetCatalogAsync(CancellationToken.None);
            var offer = catalog.Offers.Count == 0 ? null : catalog.Offers[0];
            if (offer is null)
            {
                ErrorMessage = _languageService.Get("PremiumUnavailable");
                return;
            }

            var result = await _billing.PurchaseAsync(
                offer.ProductId,
                offer.BasePlanId,
                CancellationToken.None);
            if (result.State == BillingPurchaseState.Purchased && result.Entitlement is not null)
            {
                _entitlementService.StoreVerifiedSnapshot(result.Entitlement);
                SetEntitlement(result.Entitlement);
                return;
            }

            ErrorMessage = result.State switch
            {
                BillingPurchaseState.Pending => _languageService.Get("PremiumPurchasePending"),
                BillingPurchaseState.Canceled => _languageService.Get("PremiumPurchaseCanceled"),
                _ => result.ErrorMessage ?? _languageService.Get("PremiumPurchaseFailed")
            };
        });
    }

    private async Task RestoreAsync()
    {
        await RunBillingOperationAsync(async () =>
        {
            var snapshot = await _billing.RestoreAsync(CancellationToken.None);
            if (snapshot is not null)
            {
                _entitlementService.StoreVerifiedSnapshot(snapshot);
                SetEntitlement(snapshot);
                return;
            }

            SetEntitlement(await _entitlementService.GetCurrentAsync(CancellationToken.None));
        });
    }

    private Task ManageSubscriptionAsync()
    {
        try
        {
            _billing.OpenManageSubscription();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }

        return Task.CompletedTask;
    }

    private async Task RunBillingOperationAsync(Func<Task> operation, bool showError = true)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            if (showError)
            {
                ErrorMessage = _languageService.Get("PremiumUnavailable");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetEntitlement(EntitlementSnapshot snapshot)
    {
        _entitlement = snapshot;
        OnPropertyChanged(nameof(Tier));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsPremium));
        OnPropertyChanged(nameof(StateText));
        ManageSubscriptionCommand.NotifyCanExecuteChanged();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(Description));
    }
}

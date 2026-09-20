using Anthropometry.Application.Profiles;
using Anthropometry.App.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Anthropometry.App.Tests.Composition;

public sealed class ProfileTransferServiceRegistrationTests
{
    [Fact]
    public void Registers_profile_transfer_use_cases()
    {
        IServiceCollection services = new TestServiceCollection();

        services.AddProfileTransferUseCases();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ExportProfile) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ImportProfile) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
    }

    private sealed class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {
    }
}

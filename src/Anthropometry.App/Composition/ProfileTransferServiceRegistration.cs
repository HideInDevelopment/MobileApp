using Microsoft.Extensions.DependencyInjection;
using Anthropometry.Application.Profiles;

namespace Anthropometry.App.Composition;

public static class ProfileTransferServiceRegistration
{
    public static IServiceCollection AddProfileTransferUseCases(this IServiceCollection services)
    {
        services.Add(new ServiceDescriptor(typeof(ExportProfile), typeof(ExportProfile), ServiceLifetime.Transient));
        services.Add(new ServiceDescriptor(typeof(ImportProfile), typeof(ImportProfile), ServiceLifetime.Transient));
        return services;
    }
}

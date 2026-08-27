using Sip3CX.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Sip3CX;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SIP services. Call this from any host (console, web, etc.).
    /// </summary>
    public static IServiceCollection AddSip3CxServices(
        this IServiceCollection services,
        SipTransport transport = SipTransport.Udp)
    {
        services.AddSingleton<ISipClientFactory, SipClientFactory>();

        // One SIP client per DI scope (console app uses singleton scope)
        services.AddSingleton<ISipClient>(sp =>
        {
            var factory = sp.GetRequiredService<ISipClientFactory>();
            return factory.Create(transport);
        });

        services.AddSingleton<ISipCallManager, SipCallManager>();

        return services;
    }
}

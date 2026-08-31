using Sip3CX.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Sip3CX;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SIP services. Call this from any host (console, web, etc.).
    /// </summary>
    public static IServiceCollection AddSip3CxServices(this IServiceCollection services)
    {
        services.AddSingleton<ISipClient, SipSorceryClient>();
        services.AddSingleton<ISipCallManager, SipCallManager>();

        return services;
    }
}

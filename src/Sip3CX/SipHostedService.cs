using Sip3CX.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sip3CX;

/// <summary>
/// Registers the SIP client with 3CX once at application startup
/// and unregisters cleanly on shutdown.
/// </summary>
public sealed class SipHostedService : IHostedService
{
    private readonly ISipClient _sipClient;
    private readonly SipSettings _settings;
    private readonly ILogger<SipHostedService> _logger;

    public SipHostedService(
        ISipClient sipClient,
        IOptions<SipSettings> settings,
        ILogger<SipHostedService> logger)
    {
        _sipClient = sipClient;
        _settings  = settings.Value;
        _logger    = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SIP: registering with 3CX...");

        var credentials = new SipCredentials(
            _settings.Username,
            _settings.Password,
            _settings.Domain,
            _settings.Port);

        await _sipClient.RegisterAsync(credentials, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SIP: unregistering from 3CX...");
        await _sipClient.UnregisterAsync(cancellationToken);
    }
}

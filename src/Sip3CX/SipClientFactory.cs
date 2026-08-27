using Sip3CX.Abstractions;
using Microsoft.Extensions.Logging;

namespace Sip3CX;

public sealed class SipClientFactory : ISipClientFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public SipClientFactory(ILoggerFactory loggerFactory)
        => _loggerFactory = loggerFactory;

    public ISipClient Create(SipTransport transport = SipTransport.Udp)
    {
        return transport switch
        {
            SipTransport.Udp or
            SipTransport.Tcp or
            SipTransport.WebSocket or
            SipTransport.WebSocketSecure =>
                new SipSorceryClient(
                    _loggerFactory.CreateLogger<SipSorceryClient>(),
                    _loggerFactory),

            _ => throw new NotSupportedException(
                     $"SIP transport '{transport}' is not supported.")
        };
    }
}

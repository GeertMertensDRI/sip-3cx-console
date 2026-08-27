namespace Sip3CX.Abstractions;

public interface ISipClientFactory
{
    ISipClient Create(SipTransport transport = SipTransport.Udp);
}

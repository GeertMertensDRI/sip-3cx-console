using System.Collections.Concurrent;
using Sip3CX.Abstractions;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using Microsoft.Extensions.Logging;

namespace Sip3CX;

public sealed class SipSorceryClient : ISipClient
{
    private readonly ILogger<SipSorceryClient> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private SIPTransport? _transport;
    private SIPRegistrationUserAgent? _regAgent;
    private SIPUserAgent? _userAgent;
    private readonly ConcurrentDictionary<string, SipSorceryCall> _calls = new();

    public SipClientState State { get; private set; } = SipClientState.Unregistered;

    public event EventHandler<RegistrationStateChangedEventArgs>? RegistrationStateChanged;
    public event EventHandler<CallStateChangedEventArgs>?         CallStateChanged;
    public event EventHandler<IncomingCallEventArgs>?             IncomingCall;

    public SipSorceryClient(ILogger<SipSorceryClient> logger, ILoggerFactory loggerFactory)
    {
        _logger        = logger;
        _loggerFactory = loggerFactory;
    }

    public Task RegisterAsync(SipCredentials credentials, CancellationToken ct = default)
    {
        _logger.LogInformation("Registering {Username}@{Domain}:{Port}",
            credentials.Username, credentials.Domain, credentials.Port);

        _transport = new SIPTransport();

        // 3CX typically listens on UDP 5060
        _transport.AddSIPChannel(new SIPUDPChannel(
            new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)));

        // Create a user agent for incoming calls
        _userAgent = new SIPUserAgent(_transport, null);
        _userAgent.OnIncomingCall += (ua, req) =>
        {
            var callId = req.Header.CallId;
            var caller = req.Header.From.FromURI.ToString();
            _logger.LogInformation("Incoming call from {Caller} (CallId={CallId})", caller, callId);
            IncomingCall?.Invoke(this, new IncomingCallEventArgs(callId, caller));
        };

        // Registration agent — 3CX uses standard SIP registration
        _regAgent = new SIPRegistrationUserAgent(
            _transport,
            credentials.Username,
            credentials.Password,
            credentials.Domain,
            600);

        _regAgent.RegistrationSuccessful += (_, _) =>
        {
            State = SipClientState.Registered;
            _logger.LogInformation("Registration successful.");
            RegistrationStateChanged?.Invoke(this, new(SipClientState.Registered));
        };

        _regAgent.RegistrationFailed += (uri, resp, reason) =>
        {
            State = SipClientState.Error;
            _logger.LogWarning("Registration failed: {Reason}", reason);
            RegistrationStateChanged?.Invoke(this, new(SipClientState.Error, reason));
        };

        State = SipClientState.Registering;
        RegistrationStateChanged?.Invoke(this, new(SipClientState.Registering));
        _regAgent.Start();

        return Task.CompletedTask;
    }

    public Task UnregisterAsync(CancellationToken ct = default)
    {
        _regAgent?.Stop();
        State = SipClientState.Unregistered;
        RegistrationStateChanged?.Invoke(this, new(SipClientState.Unregistered));
        _logger.LogInformation("Unregistered.");
        return Task.CompletedTask;
    }

    public async Task<ISipCall> PlaceCallAsync(string target, CancellationToken ct = default)
    {
        if (State != SipClientState.Registered)
            throw new InvalidOperationException("Not registered to the SIP server.");

        _logger.LogInformation("Placing call to {Target}", target);

        var ua = new SIPUserAgent(_transport!, null);

        // VoIPMediaSession uses the machine's default microphone & speaker via NAudio/FFmpeg
        // This replaces the removed RtpAVSession from the core SIPSorcery package
        var session = new VoIPMediaSession();
        session.AcceptRtpFromAny = true;

        var callDescriptor = new SIPCallDescriptor(
            username:      null,
            password:      null,
            uri:           target,
            from:          null,
            to:            null,
            routeSet:      null,
            customHeaders: null,
            authUsername:  null,
            callDirection: SIPCallDirection.Out,
            contentType:   SDP.SDP_MIME_CONTENTTYPE,
            content:       null,
            callReason:    SIPCallReasonEnum.Normal);

        bool callResult = await ua.Call(callDescriptor, session);

        var callId  = ua.Dialogue?.CallId ?? Guid.NewGuid().ToString();
        var sipCall = new SipSorceryCall(
            ua, callId, target,
            _loggerFactory.CreateLogger<SipSorceryCall>());

        if (callResult)
        {
            // Start audio — pipes microphone → RTP and RTP → speaker
            await session.Start();
            sipCall.MarkConnected();
            _logger.LogInformation("Call connected. CallId={CallId}", callId);
        }
        else
        {
            sipCall.MarkFailed();
            _logger.LogWarning("Call failed to connect. CallId={CallId}", callId);
        }

        _calls[callId] = sipCall;
        CallStateChanged?.Invoke(this, new(callId, sipCall.State, target));

        return sipCall;
    }

    public Task AcceptCallAsync(string callId, CancellationToken ct = default)
    {
        _logger.LogInformation("Accepting call {CallId}", callId);
        // Full answer flow: ua.Answer(uas, session) — extend as needed
        return Task.CompletedTask;
    }

    public async Task HangUpAsync(string callId, CancellationToken ct = default)
    {
        if (_calls.TryGetValue(callId, out var call))
        {
            await call.HangUpAsync(ct);
            _calls.TryRemove(callId, out _);
            CallStateChanged?.Invoke(this, new(callId, SipCallState.Ended, call.RemoteParty));
        }
    }

    public async ValueTask DisposeAsync()
    {
        _regAgent?.Stop();
        foreach (var call in _calls.Values)
            await call.DisposeAsync();
        _transport?.Dispose();
    }
}

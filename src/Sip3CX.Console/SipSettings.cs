namespace Sip3CX.ConsoleApp;

public sealed class SipSettings
{
    public string Username  { get; set; } = string.Empty;
    public string Password  { get; set; } = string.Empty;
    public string Domain    { get; set; } = string.Empty;
    public int    Port      { get; set; } = 5060;
    public string Transport { get; set; } = "Udp";
}

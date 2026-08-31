using Sip3CX;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<SipSettings>(builder.Configuration.GetSection("Sip"));
builder.Services.AddSip3CxServices();
builder.Services.AddHostedService<SipHostedService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<Sip3CX.Web.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();

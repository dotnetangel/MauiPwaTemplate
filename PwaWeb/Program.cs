using Lib.Net.Http.WebPush;
using PwaWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var fidoSection = builder.Configuration.GetSection("Fido2");
var vapidSection = builder.Configuration.GetSection("VapidKeys");
var publicKey = vapidSection.GetValue<string>("PublicKey") ?? string.Empty;
var privateKey = vapidSection.GetValue<string>("PrivateKey") ?? string.Empty;

builder.Services.AddSingleton<WebPushService>(sp => 
    new WebPushService(publicKey, privateKey, sp.GetRequiredService<ILogger<WebPushService>>()));
builder.Services.AddSingleton<WebAuthnService>();
builder.Services.AddFido2(options =>
{
    options.ServerDomain = fidoSection.GetValue<string>("ServerDomain") ?? "example.com";
    options.ServerName = fidoSection.GetValue<string>("ServerName") ?? "MauiPwaSample";
    options.Origins = new HashSet<string> { fidoSection.GetValue<string>("Origin") ?? "https://example.com" };
});

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();

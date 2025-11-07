using Lib.Net.Http.WebPush;
using PwaWeb.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var vapidSection = builder.Configuration.GetSection("VapidKeys");
var publicKey = vapidSection.GetValue<string>("PublicKey") ?? string.Empty;
var privateKey = vapidSection.GetValue<string>("PrivateKey") ?? string.Empty;

builder.Services.AddSingleton(new WebPushService(publicKey, privateKey));
builder.Services.AddSingleton(new WebAuthnService(builder.Configuration.GetSection("Fido2")));

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();

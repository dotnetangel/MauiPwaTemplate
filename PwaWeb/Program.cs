using Lib.Net.Http.WebPush;
using PwaWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5000", "https://localhost:5001" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Web Push Service
var vapidSection = builder.Configuration.GetSection("VapidKeys");
var publicKey = vapidSection.GetValue<string>("PublicKey") ?? string.Empty;
var privateKey = vapidSection.GetValue<string>("PrivateKey") ?? string.Empty;

if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
{
    builder.Logging.AddConsole().AddDebug();
    var startupLogger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("Startup");
    startupLogger.LogWarning("VAPID keys are not configured. Please set VapidKeys:PublicKey and VapidKeys:PrivateKey in appsettings.json");
    startupLogger.LogInformation("To generate VAPID keys, use: npm install -g web-push && web-push generate-vapid-keys");
}

builder.Services.AddSingleton<WebPushService>(sp => 
    new WebPushService(publicKey, privateKey, sp.GetRequiredService<ILogger<WebPushService>>()));

// Configure FIDO2/WebAuthn Service
var fidoSection = builder.Configuration.GetSection("Fido2");
builder.Services.AddScoped<WebAuthnService>();
builder.Services.AddFido2(options =>
{
    options.ServerDomain = fidoSection.GetValue<string>("ServerDomain") ?? "localhost";
    options.ServerName = fidoSection.GetValue<string>("ServerName") ?? "PWA Sample Application";
    options.Origins = new HashSet<string> 
    { 
        fidoSection.GetValue<string>("Origin") ?? "http://localhost:5000" 
    };
    options.TimestampDriftTolerance = fidoSection.GetValue<int>("TimestampDriftTolerance", 300000);
});

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseResponseCompression();

// HTTPS redirection can be enabled in production
// app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Add simple error endpoint
app.MapGet("/error", () => Results.Problem("An error occurred processing your request."));

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("PWA Web Application started");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("FIDO2 Origin: {Origin}", fidoSection.GetValue<string>("Origin"));

app.Run();

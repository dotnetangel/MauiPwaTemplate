using Microsoft.Extensions.Logging;

namespace NativeFidoMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register custom WebView handlers for Android and iOS
        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<Microsoft.Maui.Controls.WebView, Platforms.Android.CustomWebViewHandler>();
#elif IOS
            handlers.AddHandler<Microsoft.Maui.Controls.WebView, Platforms.iOS.CustomWebViewHandler>();
#endif
        });

        // Register deep link service
        builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

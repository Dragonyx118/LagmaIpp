using LagmaIpp.Services;
using LagmaIpp.ViewModels;
using LagmaIpp.Views;
using Microsoft.Extensions.Logging;

namespace LagmaIpp;

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

        // Servizi singleton
        builder.Services.AddSingleton<MqttService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<GamepadService>();  // ← NUOVO

        // Shell + pagine
        builder.Services.AddSingleton<DashboardShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

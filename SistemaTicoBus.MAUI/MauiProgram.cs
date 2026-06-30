using SistemaTicoBus.MAUI.Services;
using SistemaTicoBus.MAUI.Views;

namespace SistemaTicoBus.MAUI
{
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

            builder.Services.AddSingleton<TicoBusApiService>();

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<InicioPage>();
            builder.Services.AddTransient<MisReservasPage>();
            builder.Services.AddTransient<DetalleReservaPage>();
            builder.Services.AddTransient<PerfilPage>();

            return builder.Build();
        }
    }
}
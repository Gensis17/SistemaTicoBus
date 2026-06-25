using Microsoft.EntityFrameworkCore;
using SistemaTicoBus.BL;
using SistemaTicoBus.BL.Servicios;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.DA.Repositorios;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configuración del correo. La API es la encargada de enviar correos por Mailtrap.
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

builder.Services.AddScoped<IEmailServicio, EmailServicio>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddScoped<ViajeRepositorio>(provider =>
    new ViajeRepositorio(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    )
);

builder.Services.AddScoped<ViajeBL>();
builder.Services.AddScoped<ViajesEnCursoBL>();

var app = builder.Build();

app.UseRouting();

// Middleware simple de API Key.
// Toda petición que empiece con /api debe traer el header X-API-KEY correcto.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        string headerName = app.Configuration["ApiKey:HeaderName"] ?? "X-API-KEY";
        string configuredApiKey = app.Configuration["ApiKey:Key"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                exito = false,
                mensaje = "La API Key no está configurada en el servidor."
            });
            return;
        }

        if (!context.Request.Headers.TryGetValue(headerName, out var receivedApiKey) ||
            receivedApiKey != configuredApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                exito = false,
                mensaje = "API Key inválida o ausente."
            });
            return;
        }
    }

    await next();
});

app.MapControllers();

app.Run();
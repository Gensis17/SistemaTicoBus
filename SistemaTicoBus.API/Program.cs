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

builder.Services.AddScoped<UnidadRepositorio>(provider =>
    new UnidadRepositorio(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    )
);

builder.Services.AddScoped<PasajeroRepositorio>(provider =>
    new PasajeroRepositorio(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    )
);

builder.Services.AddScoped<ReservaRepositorio>(provider =>
    new ReservaRepositorio(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    )
);

builder.Services.AddScoped<ViajeCanceladoRepositorio>(provider =>
    new ViajeCanceladoRepositorio(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    )
);

// BL
builder.Services.AddScoped<ViajeBL>();
builder.Services.AddScoped<UnidadBL>();
builder.Services.AddScoped<ReservaBL>();
builder.Services.AddScoped<ViajeCanceladoBL>();
builder.Services.AddScoped<ViajesEnCursoBL>();

var app = builder.Build();

app.UseRouting();

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
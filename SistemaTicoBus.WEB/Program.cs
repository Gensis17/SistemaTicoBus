using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaTicoBus.BL;
using SistemaTicoBus.BL.Servicios;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.WEB.Models;
using SistemaTicoBus.WEB.Services.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings")
);

// Cliente HTTP usado por la UI para consumir la API REST con API Key.
builder.Services.AddHttpClient<ITicoBusApiClient, TicoBusApiClient>((serviceProvider, client) =>
{
    ApiSettings apiSettings = serviceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;

    client.BaseAddress = new Uri(apiSettings.BaseUrl);

    if (!string.IsNullOrWhiteSpace(apiSettings.ApiKey))
    {
        client.DefaultRequestHeaders.Add(apiSettings.HeaderName, apiSettings.ApiKey);
    }
});

builder.Services.AddScoped<IEmailServicio, EmailServicio>();
builder.Services.AddScoped<ViajeBL>();
builder.Services.AddScoped<ViajesEnCursoBL>();

builder.Services.AddScoped<ViajeRepositorio>(provider =>
    new ViajeRepositorio(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    )
);

// Se mantiene porque otros módulos del proyecto todavía lo usan.
// Para tus módulos 1 y 2, login/cambio de clave/choferes ya pasan por API.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
using Microsoft.Extensions.Options;
using SistemaTicoBus.BL.Servicios;
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

// Cliente HTTP para consumir la API
builder.Services.AddHttpClient<ITicoBusApiClient, TicoBusApiClient>((serviceProvider, client) =>
{
    ApiSettings apiSettings = serviceProvider.GetRequiredService<IOptions<ApiSettings>>().Value;

    client.BaseAddress = new Uri(apiSettings.BaseUrl);

    if (!string.IsNullOrWhiteSpace(apiSettings.ApiKey))
    {
        client.DefaultRequestHeaders.Add(apiSettings.HeaderName, apiSettings.ApiKey);
    }
});

// Servicio de correo (se mantiene)
builder.Services.AddScoped<IEmailServicio, EmailServicio>();

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
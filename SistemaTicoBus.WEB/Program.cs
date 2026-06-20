using Microsoft.EntityFrameworkCore;
using SistemaTicoBus.BL;
using SistemaTicoBus.BL.Servicios;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.DA.Repositorios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);

builder.Services.AddScoped<IEmailServicio, EmailServicio>();
builder.Services.AddScoped<ViajeBL>();
builder.Services.AddScoped<ViajesEnCursoBL>();

builder.Services.AddScoped<ViajeRepositorio>(provider =>
    new ViajeRepositorio(
        builder.Configuration.GetConnectionString("DefaultConnection")!
    )
);

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
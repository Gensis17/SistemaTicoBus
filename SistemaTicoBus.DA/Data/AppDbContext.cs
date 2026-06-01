using Microsoft.EntityFrameworkCore;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.DA.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Ruta> Rutas { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Viaje> Viajes { get; set; }
        public DbSet<Pasajero> Pasajeros { get; set; }
        public DbSet<Chofer> Choferes { get; set; }
        public DbSet<Unidad> Unidados { get; set; }

    }
}
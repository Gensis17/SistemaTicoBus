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
        public DbSet<Unidad> Unidades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ruta>(entity =>
            {
                entity.ToTable("Rutas");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.Id)
                    .HasColumnName("Id");

                entity.Property(r => r.Nombre)
                    .HasColumnName("Nombre");

                entity.Property(r => r.Origen)
                    .HasColumnName("Origen");

                entity.Property(r => r.Destino)
                    .HasColumnName("Destino");

                entity.Property(r => r.DuracionEstimada)
                    .HasColumnName("DuracionEstimada");

                entity.Property(r => r.PrecioBase)
                    .HasColumnName("PrecioBase")
                    .HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<Unidad>(entity =>
            {
                entity.ToTable("Unidades");

                entity.HasKey(u => u.Placa);

                entity.Property(u => u.Placa)
                    .HasColumnName("Placa");

                entity.Property(u => u.Modelo)
                    .HasColumnName("Modelo");

                entity.Property(u => u.AnioFabricacion)
                    .HasColumnName("AnioFabricacion");

                entity.Property(u => u.CapacidadPasajeros)
                    .HasColumnName("CapacidadPasajeros");

                entity.HasMany(u => u.Viajes)
                    .WithOne(v => v.Unidad)
                    .HasForeignKey(v => v.PlacaUnidad);
            });

            modelBuilder.Entity<Chofer>(entity =>
            {
                entity.ToTable("Choferes");

                entity.HasKey(c => c.Identificacion);

                entity.Property(c => c.Identificacion)
                    .HasColumnName("Identificacion");

                entity.Property(c => c.Nombre)
                    .HasColumnName("Nombre");

                entity.Property(c => c.Apellidos)
                    .HasColumnName("Apellidos");

                entity.Property(c => c.UsuarioId)
                    .HasColumnName("UsuarioId");

                entity.HasMany(c => c.Viajes)
                    .WithOne(v => v.Chofer)
                    .HasForeignKey(v => v.ChoferId);
            });

            modelBuilder.Entity<Pasajero>(entity =>
            {
                entity.ToTable("Pasajeros");

                entity.HasKey(p => p.Identificacion);

                entity.Property(p => p.Identificacion)
                    .HasColumnName("Identificacion");

                entity.Property(p => p.Nombre)
                    .HasColumnName("Nombre");

                entity.Property(p => p.Apellidos)
                    .HasColumnName("Apellidos");

                // Estas propiedades existen en el modelo Pasajero.cs,
                // pero NO existen como columnas en la tabla Pasajeros.
                // En la base de datos, Correo y Clave están en Usuarios.
                entity.Ignore(p => p.Correo);
                entity.Ignore(p => p.Clave);
                entity.Ignore(p => p.Rol);
            });

            modelBuilder.Entity<Viaje>(entity =>
            {
                entity.ToTable("Viajes");

                entity.HasKey(v => v.IdViaje);

                // En el modelo se llama IdViaje,
                // pero en SQL la columna real se llama NumeroViaje.
                entity.Property(v => v.IdViaje)
                    .HasColumnName("NumeroViaje")
                    .ValueGeneratedOnAdd();

                // En el modelo se llama IdRuta,
                // pero en SQL la columna real se llama RutaId.
                entity.Property(v => v.IdRuta)
                    .HasColumnName("RutaId");

                entity.Property(v => v.PlacaUnidad)
                    .HasColumnName("PlacaUnidad");

                entity.Property(v => v.ChoferId)
                    .HasColumnName("ChoferId");

                entity.Property(v => v.FechaHoraSalida)
                    .HasColumnName("FechaHoraSalida");

                entity.Property(v => v.FechaHoraLlegadaEstimada)
                    .HasColumnName("FechaHoraLlegadaEstimada");

                entity.Property(v => v.Estado)
                    .HasColumnName("Estado");

                entity.Property(v => v.MotivoCancelacion)
                    .HasColumnName("MotivoCancelacion");

                entity.HasOne(v => v.Ruta)
                    .WithMany()
                    .HasForeignKey(v => v.IdRuta);

                entity.HasOne(v => v.Unidad)
                    .WithMany(u => u.Viajes)
                    .HasForeignKey(v => v.PlacaUnidad);

                entity.HasOne(v => v.Chofer)
                    .WithMany(c => c.Viajes)
                    .HasForeignKey(v => v.ChoferId);

                entity.HasMany(v => v.Reservas)
                    .WithOne(r => r.Viaje)
                    .HasForeignKey(r => r.IdViaje);
            });

            modelBuilder.Entity<Reserva>(entity =>
            {
                entity.ToTable("Reservas");

                entity.HasKey(r => r.IdReserva);

                // En el modelo se llama IdReserva,
                // pero en SQL la columna real se llama Id.
                entity.Property(r => r.IdReserva)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                // En el modelo se llama IdViaje,
                // pero en SQL la columna real se llama ViajeId.
                entity.Property(r => r.IdViaje)
                    .HasColumnName("ViajeId");

                // En el modelo se llama IdPasajero,
                // pero en SQL la columna real se llama PasajeroId.
                entity.Property(r => r.IdPasajero)
                    .HasColumnName("PasajeroId");

                entity.Property(r => r.NumeroAsiento)
                    .HasColumnName("NumeroAsiento");

                entity.Property(r => r.MontoPagado)
                    .HasColumnName("MontoPagado")
                    .HasColumnType("decimal(10,2)");

                entity.Property(r => r.FechaReserva)
                    .HasColumnName("FechaReserva");

                entity.HasOne(r => r.Viaje)
                    .WithMany(v => v.Reservas)
                    .HasForeignKey(r => r.IdViaje);

                entity.HasOne(r => r.Pasajero)
                    .WithMany()
                    .HasForeignKey(r => r.IdPasajero);
            });
        }
    }
}
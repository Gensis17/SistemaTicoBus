using Microsoft.EntityFrameworkCore;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.DA.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Ruta> Rutas { get; set; }
    }
}
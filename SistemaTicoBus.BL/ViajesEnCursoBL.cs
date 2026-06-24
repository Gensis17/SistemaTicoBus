using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.BL
{
    public class ViajesEnCursoBL
    {
        private readonly AppDbContext _context;

        public ViajesEnCursoBL(AppDbContext context)
        {
            _context = context;
        }

        // 1. OBTENER VIAJES EN ESTADO "EN CURSO"
        public async Task<List<Viaje>> ObtenerViajesActivosAsync()
        {
            return await _context.Viajes
                .Include(v => v.Ruta)
                .Include(v => v.Unidad)
                .Include(v => v.Reservas)
                .Where(v => v.Estado == "En Curso")
                .ToListAsync();
        }

        // 2. OBTENER DETALLE DE UN VIAJE ESPECÍFICO CON SUS PASAJEROS
        public async Task<Viaje?> ObtenerDetalleViajeAsync(int idViaje)
        {
            return await _context.Viajes
                .Include(v => v.Ruta)
                .Include(v => v.Unidad)
                .Include(v => v.Reservas)
                    .ThenInclude(r => r.Pasajero)
                .FirstOrDefaultAsync(v => v.IdViaje == idViaje);
        }

        // 3. PROCESAR Y VALIDAR UNA NUEVA RESERVA
        public async Task<(bool ComponenteExitoso, string Mensaje)> RegistrarReservaAsync(int idViaje, string idPasajero, int numeroAsiento)
        {
            var viaje = await _context.Viajes
                .Include(v => v.Unidad)
                .Include(v => v.Ruta)
                .Include(v => v.Reservas)
                .FirstOrDefaultAsync(v => v.IdViaje == idViaje);

            if (viaje == null)
                return (false, "El viaje seleccionado no existe.");

            if (viaje.Estado != "En Curso")
                return (false, "No se pueden realizar reservas en un viaje que ya no está activo.");

            // Validación usando la propiedad real: CapacidadPasajeros
            if (viaje.Reservas.Count >= viaje.Unidad!.CapacidadPasajeros)
                return (false, "La unidad asignada a este viaje ya alcanzó su capacidad máxima de pasajeros.");

            // Validación: Rango de asientos válido
            if (numeroAsiento < 1 || numeroAsiento > viaje.Unidad.CapacidadPasajeros)
                return (false, $"El número de asiento elegido es inválido. Debe estar entre 1 y {viaje.Unidad.CapacidadPasajeros}.");

            // Validación: Asiento ocupado
            if (viaje.Reservas.Any(r => r.NumeroAsiento == numeroAsiento))
                return (false, $"El asiento #{numeroAsiento} ya se encuentra ocupado por otro pasajero.");

            var nuevaReserva = new Reserva
            {
                IdViaje = idViaje,
                IdPasajero = idPasajero,
                NumeroAsiento = numeroAsiento,
                MontoPagado = viaje.Ruta!.PrecioBase,
                FechaReserva = DateTime.Now
            };

            await _context.Reservas.AddAsync(nuevaReserva);
            bool guardado = await _context.SaveChangesAsync() > 0;

            return guardado ? (true, "Reserva procesada con éxito.") : (false, "Ocurrió un error interno al guardar en el servidor.");
        }

        // 4. CANCELAR RESERVA ANTES DE COMPLETAR EL VIAJE
        public async Task<bool> CancelarReservaAsync(int idReserva)
        {
            var reserva = await _context.Reservas.FindAsync(idReserva);
            if (reserva == null) return false;

            _context.Reservas.Remove(reserva);
            return await _context.SaveChangesAsync() > 0;
        }

        // 5. FINALIZAR EL VIAJE
        public async Task<bool> FinalizarViajeAsync(int idViaje)
        {
            var viaje = await _context.Viajes.FindAsync(idViaje);
            if (viaje == null || viaje.Estado != "En Curso") return false;

            viaje.Estado = "Completado";
            _context.Viajes.Update(viaje);
            return await _context.SaveChangesAsync() > 0;
        }


        public async Task<List<PasajeroCatalogoDTO>> ObtenerCatalogoPasajerosAsync()
        {
            return await _context.Pasajeros
                .Select(p => new PasajeroCatalogoDTO
                {
                    Identificacion = p.Identificacion,
                    NombreCompleto = p.Nombre + " " + p.Apellidos
                })
                .OrderBy(p => p.NombreCompleto)
                .ToListAsync();
        }
    }

    public class PasajeroCatalogoDTO
    {
        public string Identificacion { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajesEnCursoController : Controller
    {
        private readonly ViajesEnCursoBL _viajesBL;
        private readonly System.IServiceProvider _serviceProvider;

        public ViajesEnCursoController(ViajesEnCursoBL viajesBL, System.IServiceProvider serviceProvider)
        {
            _viajesBL = viajesBL;
            _serviceProvider = serviceProvider;
        }

        // GET: ViajesEnCurso
        public async Task<IActionResult> Index()
        {
            var viajesActivos = await _viajesBL.ObtenerViajesActivosAsync();
            return View(viajesActivos);
        }

        // GET: ViajesEnCurso/Detalles/
        public async Task<IActionResult> Detalles(int id)
        {
            var viaje = await _viajesBL.ObtenerDetalleViajeAsync(id);
            if (viaje == null) return NotFound();

            ViewBag.PasajerosEmbarcados = viaje.Reservas?.Count ?? 0;
            ViewBag.AsientosDisponibles = (viaje.Unidad?.CapacidadPasajeros ?? 0) - (viaje.Reservas?.Count ?? 0);
            ViewBag.TotalRecaudado = viaje.Reservas?.Sum(r => r.MontoPagado) ?? 0;

            return View(viaje);
        }

        // GET: ViajesEnCurso/Reservar
        public async Task<IActionResult> Reservar(int id)
        {
            var viaje = await _viajesBL.ObtenerDetalleViajeAsync(id);
            if (viaje == null || viaje.Estado != "En Curso") return NotFound();

            if ((viaje.Reservas?.Count ?? 0) >= (viaje.Unidad?.CapacidadPasajeros ?? 0))
            {
                TempData["Error"] = "La unidad asignada a este viaje ya alcanzó su capacidad máxima de pasajeros.";
                return RedirectToAction(nameof(Index));
            }

            var context = (SistemaTicoBus.DA.Data.AppDbContext)_serviceProvider.GetService(typeof(SistemaTicoBus.DA.Data.AppDbContext))!;

            // Mapeo y ordenación usando Nombre y Apellidos de forma combinada
            var listaPasajerosEstructurada = context.Pasajeros.Select(p => new {
                Identificacion = p.Identificacion,
                NombreCompleto = p.Nombre + " " + p.Apellidos
            }).OrderBy(p => p.NombreCompleto).ToList();

            ViewBag.Pasajeros = new SelectList(listaPasajerosEstructurada, "Identificacion", "NombreCompleto");
            ViewBag.Viaje = viaje;

            return View();
        }

        // ViajesEnCurso/Reservar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int idViaje, string idPasajero, int numeroAsiento)
        {
            var resultado = await _viajesBL.RegistrarReservaAsync(idViaje, idPasajero, numeroAsiento);

            if (resultado.ComponenteExitoso)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Detalles), new { id = idViaje });
            }

            ModelState.AddModelError("", resultado.Mensaje);

            var viaje = await _viajesBL.ObtenerDetalleViajeAsync(idViaje);
            var context = (SistemaTicoBus.DA.Data.AppDbContext)_serviceProvider.GetService(typeof(SistemaTicoBus.DA.Data.AppDbContext))!;

            var listaPasajerosEstructurada = context.Pasajeros.Select(p => new {
                Identificacion = p.Identificacion,
                NombreCompleto = p.Nombre + " " + p.Apellidos
            }).OrderBy(p => p.NombreCompleto).ToList();

            ViewBag.Pasajeros = new SelectList(listaPasajerosEstructurada, "Identificacion", "NombreCompleto", idPasajero);
            ViewBag.Viaje = viaje;

            return View();
        }

        // ViajesEnCurso/CancelarReserva
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarReserva(int idReserva, int idViaje)
        {
            bool cancelado = await _viajesBL.CancelarReservaAsync(idReserva);
            if (cancelado)
            {
                TempData["Exito"] = "La reserva fue cancelada con éxito y el número de asiento quedó liberado.";
            }
            else
            {
                TempData["Error"] = "Ocurrió un inconveniente al intentar cancelar la reserva especificada.";
            }
            return RedirectToAction(nameof(Detalles), new { id = idViaje });
        }

        // ViajesEnCurso/FinalizarViaje
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarViaje(int idViaje)
        {
            bool finalizado = await _viajesBL.FinalizarViajeAsync(idViaje);
            if (finalizado)
            {
                TempData["Exito"] = $"El viaje #{idViaje} ha cambiado a estado Completado. El arqueo de caja fue archivado.";
            }
            else
            {
                TempData["Error"] = "No se logró finalizar el viaje debido a inconsistencias en el estado actual.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
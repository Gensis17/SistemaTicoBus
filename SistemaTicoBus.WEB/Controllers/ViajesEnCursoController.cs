using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajesEnCursoController : Controller
    {
        private readonly ITicoBusApiClient _apiClient;

        public ViajesEnCursoController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            ApiResultado<List<Viaje>> resultado = await _apiClient.ObtenerViajesEnCursoAsync();

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["Error"] = resultado.Mensaje;
                return View(new List<Viaje>());
            }

            return View(resultado.Datos);
        }

        public async Task<IActionResult> Detalles(int id)
        {
            ApiResultado<Viaje> resultado = await _apiClient.ObtenerDetalleViajeEnCursoAsync(id);

            if (!resultado.Exito || resultado.Datos == null)
            {
                return NotFound();
            }

            var viaje = resultado.Datos;

            ViewBag.PasajerosEmbarcados = viaje.Reservas?.Count ?? 0;
            ViewBag.AsientosDisponibles = (viaje.Unidad?.CapacidadPasajeros ?? 0) - (viaje.Reservas?.Count ?? 0);
            ViewBag.TotalRecaudado = viaje.Reservas?.Sum(r => r.MontoPagado) ?? 0;

            return View(viaje);
        }

        public async Task<IActionResult> Reservar(int id)
        {
            ApiResultado<Viaje> resultadoViaje = await _apiClient.ObtenerDetalleViajeEnCursoAsync(id);

            if (!resultadoViaje.Exito || resultadoViaje.Datos == null || resultadoViaje.Datos.Estado != "En Curso")
            {
                return NotFound();
            }

            var viaje = resultadoViaje.Datos;

            if ((viaje.Reservas?.Count ?? 0) >= (viaje.Unidad?.CapacidadPasajeros ?? 0))
            {
                TempData["Error"] = "La unidad asignada a este viaje ya alcanzó su capacidad máxima de pasajeros.";
                return RedirectToAction(nameof(Index));
            }

            ApiResultado<List<PasajeroCatalogoDTO>> resultadoPasajeros =
                await _apiClient.ObtenerCatalogoPasajerosAsync();

            ViewBag.Pasajeros = new SelectList(
                resultadoPasajeros.Datos ?? new List<PasajeroCatalogoDTO>(),
                "Identificacion",
                "NombreCompleto"
            );

            ViewBag.Viaje = viaje;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reservar(int idViaje, string idPasajero, int numeroAsiento)
        {
            ApiResultado<object> resultado =
                await _apiClient.ReservarViajeEnCursoAsync(idViaje, idPasajero, numeroAsiento);

            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Detalles), new { id = idViaje });
            }

            ModelState.AddModelError("", resultado.Mensaje);

            ApiResultado<Viaje> resultadoViaje =
                await _apiClient.ObtenerDetalleViajeEnCursoAsync(idViaje);

            ApiResultado<List<PasajeroCatalogoDTO>> resultadoPasajeros =
                await _apiClient.ObtenerCatalogoPasajerosAsync();

            ViewBag.Pasajeros = new SelectList(
                resultadoPasajeros.Datos ?? new List<PasajeroCatalogoDTO>(),
                "Identificacion",
                "NombreCompleto",
                idPasajero
            );

            ViewBag.Viaje = resultadoViaje.Datos;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarReserva(int idReserva, int idViaje)
        {
            ApiResultado<object> resultado =
                await _apiClient.CancelarReservaViajeEnCursoAsync(idReserva);

            if (resultado.Exito)
            {
                TempData["Exito"] = "La reserva fue cancelada con éxito y el número de asiento quedó liberado.";
            }
            else
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(Detalles), new { id = idViaje });
        }

        public async Task<IActionResult> Finalizar(int id)
        {
            ApiResultado<Viaje> resultado = await _apiClient.ObtenerDetalleViajeEnCursoAsync(id);

            if (!resultado.Exito || resultado.Datos == null || resultado.Datos.Estado != "En Curso")
            {
                return NotFound();
            }

            var viaje = resultado.Datos;

            ViewBag.PasajerosEmbarcados = viaje.Reservas?.Count ?? 0;
            ViewBag.AsientosDisponibles = (viaje.Unidad?.CapacidadPasajeros ?? 0) - (viaje.Reservas?.Count ?? 0);
            ViewBag.TotalRecaudado = viaje.Reservas?.Sum(r => r.MontoPagado) ?? 0;

            return View(viaje);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarViaje(int idViaje)
        {
            ApiResultado<object> resultado =
                await _apiClient.FinalizarViajeEnCursoAsync(idViaje);

            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
            }
            else
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
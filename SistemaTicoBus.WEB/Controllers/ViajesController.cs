using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Models;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajesController : Controller
    {
        private readonly ITicoBusApiClient _apiClient;

        public ViajesController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index(string? filtro)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            ApiResultado<List<Viaje>> resultado = await _apiClient.ObtenerViajesAsync(filtro);

            var viajes = resultado.Exito && resultado.Datos != null
                ? resultado.Datos
                : new List<Viaje>();

            if (!resultado.Exito)
            {
                TempData["Error"] = resultado.Mensaje;
            }

            ViewBag.Filtro = filtro;
            await CargarListasParaVistaAsync();

            return View(viajes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(Viaje viaje)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            ApiResultado<Viaje> resultado = await _apiClient.CrearViajeAsync(viaje);

            if (resultado.Exito)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerViaje(int id)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            ApiResultado<Viaje> resultado = await _apiClient.ObtenerViajeAsync(id);

            if (!resultado.Exito || resultado.Datos == null || resultado.Datos.Estado != "Programado")
            {
                TempData["Error"] = "Solo se pueden editar viajes en estado Programado.";
                return RedirectToAction(nameof(Index));
            }

            var viaje = resultado.Datos;

            TempData["ViajeEditarId"] = viaje.IdViaje.ToString();
            TempData["ViajeEditarRutaId"] = viaje.IdRuta.ToString();
            TempData["ViajeEditarPlaca"] = viaje.PlacaUnidad;
            TempData["ViajeEditarChoferId"] = viaje.ChoferId;
            TempData["ViajeEditarSalida"] = viaje.FechaHoraSalida.ToString("yyyy-MM-ddTHH:mm");
            TempData["ViajeEditarLlegada"] = viaje.FechaHoraLlegadaEstimada.ToString("yyyy-MM-ddTHH:mm");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Viaje viaje)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            ApiResultado<Viaje> resultado = await _apiClient.EditarViajeAsync(viaje.IdViaje, viaje);

            if (resultado.Exito)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int idViaje, string motivo)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            ApiResultado<object> resultado = await _apiClient.CancelarViajeAsync(idViaje, motivo);

            if (resultado.Exito)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iniciar(int idViaje)
        {
            var rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador" && rol != "Chofer")
                return RedirectToAction("Login", "Account");

            ApiResultado<object> resultado = await _apiClient.IniciarViajeAsync(idViaje);

            if (resultado.Exito)
                TempData["Exito"] = resultado.Mensaje;
            else
                TempData["Error"] = resultado.Mensaje;

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarListasParaVistaAsync()
        {
            ApiResultado<List<Ruta>> resultadoRutas = await _apiClient.ObtenerRutasAsync(null);
            ApiResultado<List<Unidad>> resultadoUnidades = await _apiClient.ObtenerUnidadesAsync();
            ApiResultado<List<ChoferViewModel>> resultadoChoferes = await _apiClient.ObtenerChoferesAsync(null);

            ViewBag.Rutas = new SelectList(
                resultadoRutas.Exito && resultadoRutas.Datos != null
                    ? resultadoRutas.Datos
                    : new List<Ruta>(),
                "Id",
                "Nombre"
            );

            ViewBag.Unidades = new SelectList(
                resultadoUnidades.Exito && resultadoUnidades.Datos != null
                    ? resultadoUnidades.Datos
                    : new List<Unidad>(),
                "Placa",
                "Placa"
            );

            ViewBag.Choferes = new SelectList(
                resultadoChoferes.Exito && resultadoChoferes.Datos != null
                    ? resultadoChoferes.Datos.Select(c => new
                    {
                        c.Identificacion,
                        NombreCompleto = c.Nombre + " " + c.Apellidos
                    }).ToList()
                    : new List<object>(),
                "Identificacion",
                "NombreCompleto"
            );
        }
    }
}
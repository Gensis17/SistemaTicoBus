using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajesController : Controller
    {
        private readonly ITicoBusApiClient _apiClient;
        private readonly AppDbContext _context;

        public ViajesController(ITicoBusApiClient apiClient, AppDbContext context)
        {
            _apiClient = apiClient;
            _context = context;
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
            CargarListasParaVista();

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

        private void CargarListasParaVista()
        {
            ViewBag.Rutas = new SelectList(_context.Rutas.ToList(), "Id", "Nombre");
            ViewBag.Unidades = new SelectList(_context.Unidades.ToList(), "Placa", "Placa");
            ViewBag.Choferes = new SelectList(
                _context.Choferes.Select(c => new {
                    c.Identificacion,
                    NombreCompleto = c.Nombre + " " + c.Apellidos
                }).ToList(),
                "Identificacion", "NombreCompleto"
            );
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;
using System.Data;

namespace SistemaTicoBus.WEB.Controllers
{
    public class RutasController : Controller
    {
        private const string RolAdministrador = "Administrador";

        private readonly ITicoBusApiClient _apiClient;

        public RutasController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerRuta(int id)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<Ruta> resultado = await _apiClient.ObtenerRutaAsync(id);

            if (resultado.Exito && resultado.Datos != null)
            {
                TempData["RutaEditarId"] = resultado.Datos.Id.ToString();
                TempData["RutaEditarNombre"] = resultado.Datos.Nombre;
                TempData["RutaEditarOrigen"] = resultado.Datos.Origen;
                TempData["RutaEditarDestino"] = resultado.Datos.Destino;
                TempData["RutaEditarDuracionEstimada"] = resultado.Datos.DuracionEstimada.ToString(@"hh\:mm");
                TempData["RutaEditarPrecioBase"] = resultado.Datos.PrecioBase.ToString();
            }
            else
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string nombre, string origen, string destino, string duracion, decimal precioBase)
        {
            if (!UsuarioEsAdministrador())
            {
                TempData["MensajeError"] = "Acceso denegado.";
                return RedirectToAction("AdminDashboard", "Account");
            }

            if (!TimeSpan.TryParse(duracion, out TimeSpan duracionParsed))
            {
                TempData["MensajeError"] = "El formato de duración no es válido. Use hh:mm.";
                return RedirectToAction("AdminDashboard", "Account");
            }

            Ruta ruta = new Ruta
            {
                Id = id,
                Nombre = nombre,
                Origen = origen,
                Destino = destino,
                DuracionEstimada = duracionParsed,
                PrecioBase = precioBase
            };

            ApiResultado<Ruta> resultado = await _apiClient.EditarRutaAsync(id, ruta);

            if (!resultado.Exito)
                TempData["MensajeError"] = resultado.Mensaje;
            else
                TempData["MensajeExito"] = resultado.Mensaje;

            return RedirectToAction("AdminDashboard", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(string nombre, string origen, string destino, string duracion, decimal precioBase)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("AdminDashboard", "Account");
            }

            if (!TimeSpan.TryParse(duracion, out TimeSpan duracionParsed))
            {
                TempData["MensajeError"] = "El formato de duración no es válido. Use hh:mm.";
                return RedirectToAction("AdminDashboard", "Account");
            }

            Ruta ruta = new Ruta
            {
                Nombre = nombre,
                Origen = origen,
                Destino = destino,
                DuracionEstimada = duracionParsed,
                PrecioBase = precioBase
            };

            ApiResultado<Ruta> resultado = await _apiClient.CrearRutaAsync(ruta);

            if (!resultado.Exito)
                TempData["MensajeError"] = resultado.Mensaje;
            else
                TempData["MensajeExito"] = resultado.Mensaje;

            return RedirectToAction("AdminDashboard", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> ListadoRutas(string buscar)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<List<Ruta>> resultado = await _apiClient.ObtenerRutasAsync(buscar);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return View(new List<Ruta>());
            }

            return View(resultado.Datos);
        }

        [HttpGet]
        public IActionResult Index(string buscar)
        {
            return RedirectToAction("ListadoRutas", new { buscar });
        }

        private bool UsuarioEsAdministrador()
        {
            string? rol = HttpContext.Session.GetString("Rol");
            return rol == RolAdministrador;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarRuta(int id)
        {
            ApiResultado<object> resultado =
                await _apiClient.EliminarRutaAsync(id);

            if (!resultado.Exito)
                TempData["MensajeError"] = resultado.Mensaje;
            else
                TempData["MensajeExito"] = resultado.Mensaje;

            return RedirectToAction("AdminDashboard", "Account");
        }
    }
}
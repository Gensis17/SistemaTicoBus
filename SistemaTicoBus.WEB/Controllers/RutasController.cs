using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class RutasController : Controller
    {
        private const string RolAdministrador = "Administrador";
        private const string RolChofer = "Chofer";

        private readonly ITicoBusApiClient _apiClient;

        public RutasController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Index(string? buscar)
        {
            if (!UsuarioPuedeAcceder())
            {
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction(nameof(ListadoRutas), new { buscar });
        }

        [HttpGet]
        public async Task<IActionResult> ListadoRutas(string? buscar)
        {
            if (!UsuarioPuedeAcceder())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<List<Ruta>> resultado = await _apiClient.ObtenerRutasAsync(buscar);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                ViewBag.Busqueda = buscar;
                return View(new List<Ruta>());
            }

            ViewBag.Busqueda = buscar;
            return View(resultado.Datos);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerRuta(int id)
        {
            if (!UsuarioPuedeAcceder())
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

            return RedireccionarDespuesDeGuardar();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            string? nombre,
            string? origen,
            string? destino,
            string? duracion,
            decimal precioBase)
        {
            if (!UsuarioPuedeAcceder())
            {
                return RedirectToAction("Login", "Account");
            }

            nombre = nombre?.Trim() ?? string.Empty;
            origen = origen?.Trim() ?? string.Empty;
            destino = destino?.Trim() ?? string.Empty;
            duracion = duracion?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(origen) ||
                string.IsNullOrWhiteSpace(destino) ||
                string.IsNullOrWhiteSpace(duracion) ||
                precioBase <= 0)
            {
                TempData["MensajeError"] = "Nombre, origen, destino, duración y precio base son requeridos.";
                return RedireccionarDespuesDeGuardar();
            }

            if (!TimeSpan.TryParse(duracion, out TimeSpan duracionParsed))
            {
                TempData["MensajeError"] = "El formato de duración no es válido. Use HH:mm.";
                return RedireccionarDespuesDeGuardar();
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
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedireccionarDespuesDeGuardar();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            string? nombre,
            string? origen,
            string? destino,
            string? duracion,
            decimal precioBase)
        {
            if (!UsuarioPuedeAcceder())
            {
                return RedirectToAction("Login", "Account");
            }

            nombre = nombre?.Trim() ?? string.Empty;
            origen = origen?.Trim() ?? string.Empty;
            destino = destino?.Trim() ?? string.Empty;
            duracion = duracion?.Trim() ?? string.Empty;

            if (id <= 0 ||
                string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(origen) ||
                string.IsNullOrWhiteSpace(destino) ||
                string.IsNullOrWhiteSpace(duracion) ||
                precioBase <= 0)
            {
                TempData["MensajeError"] = "Verifique los datos de la ruta.";
                return RedireccionarDespuesDeGuardar();
            }

            if (!TimeSpan.TryParse(duracion, out TimeSpan duracionParsed))
            {
                TempData["MensajeError"] = "El formato de duración no es válido. Use HH:mm.";
                return RedireccionarDespuesDeGuardar();
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
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedireccionarDespuesDeGuardar();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarRuta(int id)
        {
            if (!UsuarioEsAdministrador())
            {
                if (!UsuarioPuedeAcceder())
                {
                    return RedirectToAction("Login", "Account");
                }

                TempData["MensajeError"] = "Solo el administrador puede eliminar rutas.";
                return RedirectToAction(nameof(ListadoRutas));
            }

            ApiResultado<object> resultado = await _apiClient.EliminarRutaAsync(id);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        private bool UsuarioPuedeAcceder()
        {
            string rol = ObtenerRolSesion();

            return string.Equals(rol, RolAdministrador, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rol, RolChofer, StringComparison.OrdinalIgnoreCase);
        }

        private bool UsuarioEsAdministrador()
        {
            string rol = ObtenerRolSesion();

            return string.Equals(rol, RolAdministrador, StringComparison.OrdinalIgnoreCase);
        }

        private string ObtenerRolSesion()
        {
            return (HttpContext.Session.GetString("Rol") ?? string.Empty).Trim();
        }

        private IActionResult RedireccionarDespuesDeGuardar()
        {
            string rol = ObtenerRolSesion();

            if (string.Equals(rol, RolAdministrador, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AdminDashboard", "Account");
            }

            if (string.Equals(rol, RolChofer, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(ListadoRutas));
            }

            return RedirectToAction("Login", "Account");
        }
    }
}
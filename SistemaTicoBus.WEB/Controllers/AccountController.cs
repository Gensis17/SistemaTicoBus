using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Models;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class AccountController : Controller
    {
        private const string RolAdministrador = "Administrador";
        private const string RolChofer = "Chofer";
        private const string RolPasajero = "Pasajero";

        private readonly ITicoBusApiClient _apiClient;

        public AccountController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // La UI ya no consulta SQL: manda usuario y clave a la API con API Key.
            model.Username = model.Username?.Trim() ?? string.Empty;
            model.Password = model.Password?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResultado<LoginApiDatos> resultado = await _apiClient.LoginAsync(model);

            if (!resultado.Exito || resultado.Datos == null)
            {
                ModelState.AddModelError("", resultado.Mensaje);
                return View(model);
            }

            HttpContext.Session.SetInt32("UsuarioId", resultado.Datos.UsuarioId);
            HttpContext.Session.SetString("NombreUsuario", resultado.Datos.NombreUsuario);
            HttpContext.Session.SetString("Rol", resultado.Datos.Rol);

            if (resultado.Datos.Rol == RolAdministrador)
            {
                return RedirectToAction(nameof(AdminDashboard));
            }

            if (resultado.Datos.Rol == RolChofer)
            {
                return RedirectToAction(nameof(ChoferDashboard));
            }

            if (resultado.Datos.Rol == RolPasajero)
            {
                return RedirectToAction(nameof(PasajeroDashboard));
            }

            HttpContext.Session.Clear();
            ModelState.AddModelError("", "El rol del usuario no es válido.");
            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            string nombreUsuario = HttpContext.Session.GetString("NombreUsuario") ?? string.Empty;

            return View(new ChangePasswordViewModel
            {
                Nombre = nombreUsuario
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // La UI manda el cambio de clave a la API. La API valida y actualiza.
            model.Nombre = model.Nombre?.Trim() ?? string.Empty;
            model.ClaveActual = model.ClaveActual?.Trim() ?? string.Empty;
            model.NuevaClave = model.NuevaClave?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApiResultado<CambioClaveApiDatos> resultado = await _apiClient.CambiarClaveAsync(model);

            if (!resultado.Exito || resultado.Datos == null)
            {
                ModelState.AddModelError("", resultado.Mensaje);
                return View(model);
            }

            TempData["MensajeExito"] = resultado.Mensaje;

            if (resultado.Datos.Rol == RolAdministrador)
            {
                return RedirectToAction(nameof(AdminDashboard));
            }

            if (resultado.Datos.Rol == RolChofer)
            {
                return RedirectToAction(nameof(ChoferDashboard));
            }

            if (resultado.Datos.Rol == RolPasajero)
            {
                return RedirectToAction(nameof(PasajeroDashboard), new { tab = "viajes" });
            }

            return RedirectToAction(nameof(ChangePassword));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> AdminDashboard(string? buscar)
        {
            if (!UsuarioTieneRol(RolAdministrador))
            {
                return RedirectToAction(nameof(Login));
            }

            string nombre = HttpContext.Session.GetString("NombreUsuario") ?? "Administrador General";
            ViewBag.BusquedaActual = buscar;

            var model = new AdminDashboardViewModel
            {
                NombreCompleto = nombre,
                Identificacion = "ADM-001",
                Rol = RolAdministrador,
                Rutas = new List<Ruta>(),
                Unidades = new List<Unidad>()
            };

            ApiResultado<List<Ruta>> resultadoRutas = await _apiClient.ObtenerRutasAsync(buscar);
            ApiResultado<List<Unidad>> resultadoUnidades = await _apiClient.ObtenerUnidadesAsync();

            if (resultadoRutas.Exito && resultadoRutas.Datos != null)
            {
                model.Rutas = resultadoRutas.Datos;
            }
            else
            {
                TempData["MensajeError"] = resultadoRutas.Mensaje;
            }

            if (resultadoUnidades.Exito && resultadoUnidades.Datos != null)
            {
                model.Unidades = resultadoUnidades.Datos;
            }
            else
            {
                TempData["MensajeError"] = resultadoUnidades.Mensaje;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarRuta(string Origen, string Destino, string DuracionEstimada, decimal PrecioBase)
        {
            if (!UsuarioTieneRol(RolAdministrador))
            {
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(Origen) ||
                string.IsNullOrWhiteSpace(Destino) ||
                string.IsNullOrWhiteSpace(DuracionEstimada) ||
                PrecioBase <= 0)
            {
                TempData["MensajeError"] = "Origen, destino, duración y precio base son requeridos.";
                return RedirectToAction(nameof(AdminDashboard));
            }

            if (!TimeSpan.TryParse(DuracionEstimada, out TimeSpan duracion))
            {
                TempData["MensajeError"] = "La duración estimada debe tener formato HH:mm.";
                return RedirectToAction(nameof(AdminDashboard));
            }

            var nuevaRuta = new Ruta
            {
                Nombre = $"Ruta {Origen.Trim()} - {Destino.Trim()}",
                Origen = Origen.Trim(),
                Destino = Destino.Trim(),
                DuracionEstimada = duracion,
                PrecioBase = PrecioBase
            };

            ApiResultado<Ruta> resultado = await _apiClient.CrearRutaAsync(nuevaRuta);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return RedirectToAction(nameof(AdminDashboard));
            }

            TempData["MensajeExito"] = resultado.Mensaje;
            return RedirectToAction(nameof(AdminDashboard));
        }

        public async Task<IActionResult> ChoferDashboard()
        {
            if (!UsuarioTieneRol(RolChofer))
            {
                return RedirectToAction(nameof(Login));
            }

            int? usuarioId = HttpContext.Session.GetInt32("UsuarioId");

            if (!usuarioId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            ApiResultado<ChoferDashboardViewModel> resultado =
                await _apiClient.ObtenerDashboardChoferAsync(usuarioId.Value);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;

                return View(new ChoferDashboardViewModel
                {
                    Identificacion = "No disponible",
                    NombreCompleto = "Chofer",
                    Rol = RolChofer,
                    Viajes = new List<ViajeAsignadoDTO>()
                });
            }

            return View(resultado.Datos);
        }

        public async Task<IActionResult> PasajeroDashboard(string tab = "viajes")
        {
            if (!UsuarioTieneRol(RolPasajero))
            {
                return RedirectToAction(nameof(Login));
            }

            string nombreUsuario = HttpContext.Session.GetString("NombreUsuario") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                return RedirectToAction(nameof(Login));
            }

            ApiResultado<List<Reserva>> resultado =
                await _apiClient.ObtenerMisViajesAsync(nombreUsuario);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                ViewBag.Tab = tab;
                return View(new List<Reserva>());
            }

            ViewBag.Tab = tab;
            return View(resultado.Datos);
        }

        private bool UsuarioTieneRol(string rolRequerido)
        {
            string? rolSesion = HttpContext.Session.GetString("Rol");
            return rolSesion == rolRequerido;
        }
    }
}
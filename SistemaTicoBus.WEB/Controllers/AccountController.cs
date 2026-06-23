using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.BL;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.WEB.Models;
using SistemaTicoBus.WEB.Services.Api;
using System.Data;

namespace SistemaTicoBus.WEB.Controllers
{
    public class AccountController : Controller
    {
        private const string RolAdministrador = "Administrador";
        private const string RolChofer = "Chofer";
        private const string RolPasajero = "Pasajero";

        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly ITicoBusApiClient _apiClient;

        public AccountController(
            IConfiguration configuration,
            AppDbContext context,
            ITicoBusApiClient apiClient)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _context = context;
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
        public IActionResult AdminDashboard(string? buscar)
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
                Rutas = new List<SistemaTicoBus.MODEL.Entidades.Ruta>(),
                Unidades = new List<SistemaTicoBus.MODEL.Entidades.Unidad>()
            };

            try
            {
                var queryRutas = _context.Rutas.AsQueryable();

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string textoBusqueda = buscar.ToLower().Trim();

                    queryRutas = queryRutas.Where(r =>
                        r.Origen.ToLower().Contains(textoBusqueda) ||
                        r.Destino.ToLower().Contains(textoBusqueda) ||
                        r.Nombre.ToLower().Contains(textoBusqueda)
                    );
                }

                model.Rutas = queryRutas.ToList();

                UnidadBL unidadBL = new UnidadBL();
                model.Unidades = unidadBL.Listar();
            }
            catch
            {
                TempData["MensajeError"] = "No se pudieron cargar rutas o unidades. Revise la conexión a la base de datos.";
            }

            return View(model);
        }

        [HttpPost]
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

            var nuevaRuta = new SistemaTicoBus.MODEL.Entidades.Ruta
            {
                Nombre = $"Ruta {Origen.Trim()} - {Destino.Trim()}",
                Origen = Origen.Trim(),
                Destino = Destino.Trim(),
                DuracionEstimada = duracion,
                PrecioBase = PrecioBase
            };

            _context.Rutas.Add(nuevaRuta);
            await _context.SaveChangesAsync();

            TempData["MensajeExito"] = "Ruta registrada correctamente.";
            return RedirectToAction(nameof(AdminDashboard));
        }

        public IActionResult ChoferDashboard()
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

            try
            {
                ChoferDashboardViewModel model = ObtenerDatosDashboardChofer(usuarioId.Value);
                return View(model);
            }
            catch (SqlException)
            {
                TempData["MensajeError"] = "No se pudieron cargar los viajes del chofer. Revise la conexión a la base de datos.";

                return View(new ChoferDashboardViewModel
                {
                    Identificacion = "No disponible",
                    NombreCompleto = "Chofer",
                    Rol = RolChofer,
                    Viajes = new List<ViajeAsignadoDTO>()
                });
            }
        }

        public IActionResult PasajeroDashboard(string tab = "viajes")
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

            ReservaRepositorio repo = new ReservaRepositorio();
            var misViajes = repo.ObtenerReservasPorPasajero(nombreUsuario);

            ViewBag.Tab = tab;
            return View(misViajes);
        }

        private bool UsuarioTieneRol(string rolRequerido)
        {
            string? rolSesion = HttpContext.Session.GetString("Rol");
            return rolSesion == rolRequerido;
        }

        private ChoferDashboardViewModel ObtenerDatosDashboardChofer(int usuarioId)
        {
            ChoferDashboardViewModel model = new ChoferDashboardViewModel
            {
                Identificacion = "No disponible",
                NombreCompleto = "Chofer",
                Rol = RolChofer,
                Viajes = new List<ViajeAsignadoDTO>()
            };

            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string choferQuery = @"
                SELECT 
                    Identificacion,
                    Nombre,
                    Apellidos
                FROM Choferes
                WHERE UsuarioId = @UsuarioId";

            string identificacionChofer = string.Empty;

            using (SqlCommand command = new SqlCommand(choferQuery, connection))
            {
                command.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;

                using SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    identificacionChofer = reader["Identificacion"].ToString() ?? string.Empty;
                    model.Identificacion = identificacionChofer;
                    model.NombreCompleto = $"{reader["Nombre"]} {reader["Apellidos"]}";
                }
            }

            if (string.IsNullOrWhiteSpace(identificacionChofer))
            {
                return model;
            }

            string viajesQuery = @"
                SELECT 
                    v.NumeroViaje,
                    r.Nombre AS Ruta,
                    v.PlacaUnidad,
                    v.FechaHoraSalida,
                    v.Estado,
                    u.CapacidadPasajeros,
                    (
                        SELECT COUNT(*) 
                        FROM Reservas re 
                        WHERE re.ViajeId = v.NumeroViaje
                    ) AS AsientosOcupados
                FROM Viajes v
                INNER JOIN Rutas r ON v.RutaId = r.Id
                INNER JOIN Unidades u ON v.PlacaUnidad = u.Placa
                WHERE v.ChoferId = @ChoferId
                ORDER BY v.FechaHoraSalida DESC";

            using (SqlCommand command = new SqlCommand(viajesQuery, connection))
            {
                command.Parameters.Add("@ChoferId", SqlDbType.VarChar, 30).Value = identificacionChofer;

                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    model.Viajes.Add(new ViajeAsignadoDTO
                    {
                        IdViaje = reader["NumeroViaje"].ToString() ?? string.Empty,
                        Ruta = reader["Ruta"].ToString() ?? string.Empty,
                        UnidadPlaca = reader["PlacaUnidad"].ToString() ?? string.Empty,
                        HorarioSalida = Convert.ToDateTime(reader["FechaHoraSalida"]).ToString("dd/MM/yyyy HH:mm"),
                        Ocupacion = $"{reader["AsientosOcupados"]}/{reader["CapacidadPasajeros"]}",
                        Estado = reader["Estado"].ToString() ?? string.Empty
                    });
                }
            }

            return model;
        }
    }
}
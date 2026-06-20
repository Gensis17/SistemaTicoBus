using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.BL;
using SistemaTicoBus.BL.Servicios;
using SistemaTicoBus.DA.Data;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.WEB.Models;
using System.Data;

namespace SistemaTicoBus.WEB.Controllers
{
    public class AccountController : Controller
    {
        private const string RolAdministrador = "Administrador";
        private const string RolChofer = "Chofer";
        private const string RolPasajero = "Pasajero";
        private const int IntentosMaximos = 2;
        private const int MinutosBloqueo = 3;

        private readonly string _connectionString;
        private readonly IEmailServicio _emailServicio;
        private readonly AppDbContext _context;

        public AccountController(IConfiguration configuration, IEmailServicio emailServicio, AppDbContext context)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _emailServicio = emailServicio;
            _context = context;
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
            model.Username = model.Username?.Trim() ?? string.Empty;
            model.Password = model.Password?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            UsuarioLogin? usuario;

            try
            {
                usuario = ObtenerUsuarioLogin(model.Username);
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "No se pudo conectar con la base de datos. Verifique la conexión y que TicoBusDB exista.");
                return View(model);
            }

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            if (usuario.Rol == RolAdministrador)
            {
                if (usuario.IntentosFallidos > 0 || usuario.BloqueadoHasta.HasValue)
                {
                    ResetearIntentos(usuario.Id);
                }
            }
            else if (usuario.BloqueadoHasta.HasValue)
            {
                if (usuario.BloqueadoHasta.Value > DateTime.Now)
                {
                    await IntentarEnviarCorreoCuentaBloqueadaAsync(
                        usuario.Correo,
                        usuario.NombreUsuario,
                        usuario.BloqueadoHasta.Value
                    );

                    TimeSpan tiempoRestante = usuario.BloqueadoHasta.Value - DateTime.Now;

                    ModelState.AddModelError(
                        "",
                        $"Cuenta bloqueada. Intente de nuevo en {tiempoRestante.Minutes:00}:{tiempoRestante.Seconds:00}."
                    );

                    return View(model);
                }

                ResetearIntentos(usuario.Id);
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
            }

            if (usuario.Clave == model.Password)
            {
                ResetearIntentos(usuario.Id);

                HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);
                HttpContext.Session.SetString("Rol", usuario.Rol);

                await IntentarEnviarCorreoInicioSesionAsync(usuario.Correo, usuario.NombreUsuario);

                if (usuario.Rol == RolAdministrador)
                {
                    return RedirectToAction(nameof(AdminDashboard));
                }

                if (usuario.Rol == RolChofer)
                {
                    return RedirectToAction(nameof(ChoferDashboard));
                }

                if (usuario.Rol == RolPasajero)
                {
                    return RedirectToAction(nameof(PasajeroDashboard));
                }

                HttpContext.Session.Clear();
                ModelState.AddModelError("", "El rol del usuario no es válido.");
                return View(model);
            }

            if (usuario.Rol == RolAdministrador)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            int nuevosIntentos = usuario.IntentosFallidos + 1;

            if (nuevosIntentos >= IntentosMaximos)
            {
                DateTime bloqueadoHasta = DateTime.Now.AddMinutes(MinutosBloqueo);

                BloquearUsuario(usuario.Id, bloqueadoHasta);

                await IntentarEnviarCorreoCuentaBloqueadaAsync(
                    usuario.Correo,
                    usuario.NombreUsuario,
                    bloqueadoHasta
                );

                ModelState.AddModelError("", "Demasiados intentos fallidos. Cuenta bloqueada por 3 minutos.");
                return View(model);
            }

            RegistrarIntentoFallido(usuario.Id, nuevosIntentos);

            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
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
            model.Nombre = model.Nombre?.Trim() ?? string.Empty;
            model.ClaveActual = model.ClaveActual?.Trim() ?? string.Empty;
            model.NuevaClave = model.NuevaClave?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            UsuarioLogin? usuario;

            try
            {
                usuario = ObtenerUsuarioLogin(model.Nombre);
            }
            catch (SqlException)
            {
                ModelState.AddModelError("", "No se pudo conectar con la base de datos.");
                return View(model);
            }

            if (usuario == null)
            {
                ModelState.AddModelError("", "No existe un usuario con ese nombre.");
                return View(model);
            }

            if (usuario.Rol != RolChofer && usuario.Rol != RolPasajero)
            {
                ModelState.AddModelError("", "El cambio de clave solo está habilitado para usuarios Chofer y Pasajero.");
                return View(model);
            }

            if (usuario.Clave != model.ClaveActual)
            {
                ModelState.AddModelError("", "La clave actual no es correcta.");
                return View(model);
            }

            if (usuario.Clave == model.NuevaClave)
            {
                ModelState.AddModelError("", "La nueva clave debe ser diferente a la clave actual.");
                return View(model);
            }

            ActualizarClave(usuario.Id, model.NuevaClave);
            ResetearIntentos(usuario.Id);

            await IntentarEnviarCorreoCambioClaveAsync(usuario.Correo, usuario.NombreUsuario);

            TempData["MensajeExito"] = "La clave fue actualizada correctamente.";

            if (usuario.Rol == RolChofer)
            {
                return RedirectToAction(nameof(ChoferDashboard));
            }

            if (usuario.Rol == RolPasajero)
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
            catch (Exception)
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

        private UsuarioLogin? ObtenerUsuarioLogin(string nombreUsuario)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT 
                        u.Id,
                        u.NombreUsuario,
                        u.Clave,
                        u.Correo,
                        r.Nombre AS RolNombre,
                        u.BloqueadoHasta,
                        u.IntentosFallidos
                    FROM Usuarios u
                    INNER JOIN Roles r ON u.RolId = r.Id
                    WHERE u.NombreUsuario = @NombreUsuario";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario.Trim();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        return new UsuarioLogin
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            NombreUsuario = reader["NombreUsuario"].ToString() ?? string.Empty,
                            Clave = reader["Clave"].ToString() ?? string.Empty,
                            Correo = reader["Correo"].ToString() ?? string.Empty,
                            Rol = reader["RolNombre"].ToString() ?? string.Empty,
                            BloqueadoHasta = reader["BloqueadoHasta"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["BloqueadoHasta"]),
                            IntentosFallidos = Convert.ToInt32(reader["IntentosFallidos"])
                        };
                    }
                }
            }
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

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
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

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            identificacionChofer = reader["Identificacion"].ToString() ?? string.Empty;
                            model.Identificacion = identificacionChofer;
                            model.NombreCompleto = $"{reader["Nombre"]} {reader["Apellidos"]}";
                        }
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

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
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
                }
            }

            return model;
        }

        private async Task IntentarEnviarCorreoInicioSesionAsync(string correo, string nombreUsuario)
        {
            string asunto = $"Inicio de sesión — {nombreUsuario}";
            string cuerpo = $"Usted inició sesión el día {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}.";
            await IntentarEnviarCorreoAsync(correo, asunto, cuerpo);
        }

        private async Task IntentarEnviarCorreoCuentaBloqueadaAsync(string correo, string nombreUsuario, DateTime fechaReintento)
        {
            string asunto = "Cuenta bloqueada";
            string cuerpo =
                $"La cuenta {nombreUsuario} está bloqueada por 3 minutos. " +
                $"Puede reintentar el {fechaReintento:dd/MM/yyyy} a las {fechaReintento:HH:mm}.";

            await IntentarEnviarCorreoAsync(correo, asunto, cuerpo);
        }

        private async Task IntentarEnviarCorreoCambioClaveAsync(string correo, string nombreUsuario)
        {
            string asunto = $"Cambio de clave — {nombreUsuario}";
            string cuerpo = $"La clave de su cuenta fue actualizada el día {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm}.";
            await IntentarEnviarCorreoAsync(correo, asunto, cuerpo);
        }

        private async Task IntentarEnviarCorreoAsync(string correo, string asunto, string cuerpo)
        {
            try
            {
                await _emailServicio.EnviarCorreoAsync(correo, asunto, cuerpo);
            }
            catch
            {
                TempData["MensajeAdvertencia"] = "La operación se realizó, pero no se pudo enviar el correo. Revise la configuración de Mailtrap.";
            }
        }

        private void RegistrarIntentoFallido(int usuarioId, int intentosFallidos)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    UPDATE Usuarios
                    SET IntentosFallidos = @IntentosFallidos
                    WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@IntentosFallidos", SqlDbType.Int).Value = intentosFallidos;
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void BloquearUsuario(int usuarioId, DateTime bloqueadoHasta)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    UPDATE Usuarios
                    SET 
                        IntentosFallidos = @IntentosFallidos,
                        BloqueadoHasta = @BloqueadoHasta
                    WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@IntentosFallidos", SqlDbType.Int).Value = IntentosMaximos;
                    command.Parameters.Add("@BloqueadoHasta", SqlDbType.DateTime).Value = bloqueadoHasta;
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void ResetearIntentos(int usuarioId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    UPDATE Usuarios
                    SET 
                        IntentosFallidos = 0,
                        BloqueadoHasta = NULL
                    WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
                    command.ExecuteNonQuery();
                }
            }
        }

        private void ActualizarClave(int usuarioId, string nuevaClave)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    UPDATE Usuarios
                    SET Clave = @NuevaClave
                    WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@NuevaClave", SqlDbType.VarChar, 255).Value = nuevaClave.Trim();
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
                    command.ExecuteNonQuery();
                }
            }
        }

        private class UsuarioLogin
        {
            public int Id { get; set; }
            public string NombreUsuario { get; set; } = string.Empty;
            public string Clave { get; set; } = string.Empty;
            public string Correo { get; set; } = string.Empty;
            public string Rol { get; set; } = string.Empty;
            public DateTime? BloqueadoHasta { get; set; }
            public int IntentosFallidos { get; set; }
        }
    }
}
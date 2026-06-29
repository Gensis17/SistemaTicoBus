using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL.Servicios;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string RolAdministrador = "Administrador";
        private const string RolChofer = "Chofer";
        private const string RolPasajero = "Pasajero";
        private const int IntentosMaximos = 2;
        private const int MinutosBloqueo = 3;

        private readonly string _connectionString;
        private readonly IEmailServicio _emailServicio;

        public AuthController(IConfiguration configuration, IEmailServicio emailServicio)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _emailServicio = emailServicio;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiRespuesta<LoginRespuesta>>> Login(LoginSolicitud solicitud)
        {
            // Esta validación queda en API porque la UI ya no debe validar contra BD directamente.
            solicitud.Username = solicitud.Username?.Trim() ?? string.Empty;
            solicitud.Password = solicitud.Password?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiRespuesta<LoginRespuesta>.Error("Debe indicar usuario y contraseña."));
            }

            UsuarioLogin? usuario;

            try
            {
                usuario = ObtenerUsuarioLogin(solicitud.Username);
            }
            catch (SqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiRespuesta<LoginRespuesta>.Error("No se pudo conectar con la base de datos.")
                );
            }

            if (usuario == null)
            {
                return BadRequest(ApiRespuesta<LoginRespuesta>.Error("Usuario o contraseña incorrectos."));
            }

            if (usuario.Rol == RolAdministrador)
            {
                // El administrador nunca debe quedar bloqueado.
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

                    TimeSpan restante = usuario.BloqueadoHasta.Value - DateTime.Now;

                    return BadRequest(ApiRespuesta<LoginRespuesta>.Error(
                        $"Cuenta bloqueada. Intente de nuevo en {restante.Minutes:00}:{restante.Seconds:00}."
                    ));
                }

                // Si el bloqueo ya venció, se limpia antes de volver a validar.
                ResetearIntentos(usuario.Id);
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
            }

            if (usuario.Clave == solicitud.Password)
            {
                ResetearIntentos(usuario.Id);

                await IntentarEnviarCorreoInicioSesionAsync(usuario.Correo, usuario.NombreUsuario);

                var respuesta = new LoginRespuesta
                {
                    UsuarioId = usuario.Id,
                    NombreUsuario = usuario.NombreUsuario,
                    Rol = usuario.Rol
                };

                return Ok(ApiRespuesta<LoginRespuesta>.Ok(respuesta, "Inicio de sesión correcto."));
            }

            if (usuario.Rol == RolAdministrador)
            {
                return BadRequest(ApiRespuesta<LoginRespuesta>.Error("Usuario o contraseña incorrectos."));
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

                return BadRequest(ApiRespuesta<LoginRespuesta>.Error(
                    "Demasiados intentos fallidos. Cuenta bloqueada por 3 minutos."
                ));
            }

            RegistrarIntentoFallido(usuario.Id, nuevosIntentos);

            return BadRequest(ApiRespuesta<LoginRespuesta>.Error("Usuario o contraseña incorrectos."));
        }

        [HttpPost("cambiar-clave")]
        public async Task<ActionResult<ApiRespuesta<CambioClaveRespuesta>>> CambiarClave(CambioClaveSolicitud solicitud)
        {
            // Cambio de clave también pasa por API con API Key.
            solicitud.Nombre = solicitud.Nombre?.Trim() ?? string.Empty;
            solicitud.ClaveActual = solicitud.ClaveActual?.Trim() ?? string.Empty;
            solicitud.NuevaClave = solicitud.NuevaClave?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiRespuesta<CambioClaveRespuesta>.Error(
                    "Debe completar usuario, clave actual y nueva clave."
                ));
            }

            UsuarioLogin? usuario;

            try
            {
                usuario = ObtenerUsuarioLogin(solicitud.Nombre);
            }
            catch (SqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiRespuesta<CambioClaveRespuesta>.Error("No se pudo conectar con la base de datos.")
                );
            }

            if (usuario == null)
            {
                return BadRequest(ApiRespuesta<CambioClaveRespuesta>.Error("No existe un usuario con ese nombre."));
            }

            if (usuario.Rol != RolAdministrador && usuario.Rol != RolChofer && usuario.Rol != RolPasajero)
            {
                return BadRequest(ApiRespuesta<CambioClaveRespuesta>.Error(
                    "El rol del usuario no es válido para cambiar la clave."
                ));
            }

            if (usuario.Clave != solicitud.ClaveActual)
            {
                return BadRequest(ApiRespuesta<CambioClaveRespuesta>.Error("La clave actual no es correcta."));
            }

            if (usuario.Clave == solicitud.NuevaClave)
            {
                return BadRequest(ApiRespuesta<CambioClaveRespuesta>.Error(
                    "La nueva clave debe ser diferente a la clave actual."
                ));
            }

            try
            {
                ActualizarClave(usuario.Id, solicitud.NuevaClave);
                ResetearIntentos(usuario.Id);
            }
            catch (SqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiRespuesta<CambioClaveRespuesta>.Error("No se pudo actualizar la clave en la base de datos.")
                );
            }

            await IntentarEnviarCorreoCambioClaveAsync(usuario.Correo, usuario.NombreUsuario);

            return Ok(ApiRespuesta<CambioClaveRespuesta>.Ok(
                new CambioClaveRespuesta
                {
                    Rol = usuario.Rol
                },
                "La clave fue actualizada correctamente."
            ));
        }

        private UsuarioLogin? ObtenerUsuarioLogin(string nombreUsuario)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
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

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;

            using SqlDataReader reader = command.ExecuteReader();

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

        private void RegistrarIntentoFallido(int usuarioId, int intentosFallidos)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Usuarios
                SET IntentosFallidos = @IntentosFallidos
                WHERE Id = @Id";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@IntentosFallidos", SqlDbType.Int).Value = intentosFallidos;
            command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
            command.ExecuteNonQuery();
        }

        private void BloquearUsuario(int usuarioId, DateTime bloqueadoHasta)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Usuarios
                SET IntentosFallidos = @IntentosFallidos,
                    BloqueadoHasta = @BloqueadoHasta
                WHERE Id = @Id";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@IntentosFallidos", SqlDbType.Int).Value = IntentosMaximos;
            command.Parameters.Add("@BloqueadoHasta", SqlDbType.DateTime).Value = bloqueadoHasta;
            command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
            command.ExecuteNonQuery();
        }

        private void ResetearIntentos(int usuarioId)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Usuarios
                SET IntentosFallidos = 0,
                    BloqueadoHasta = NULL
                WHERE Id = @Id";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
            command.ExecuteNonQuery();
        }

        private void ActualizarClave(int usuarioId, string nuevaClave)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Usuarios
                SET Clave = @NuevaClave
                WHERE Id = @Id";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@NuevaClave", SqlDbType.VarChar, 255).Value = nuevaClave;
            command.Parameters.Add("@Id", SqlDbType.Int).Value = usuarioId;
            command.ExecuteNonQuery();
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
                // El correo no debe botar la operación principal.
                // Si Mailtrap está mal configurado, el login/cambio de clave sigue respondiendo.
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

    public class LoginSolicitud
    {
        [Required(ErrorMessage = "El usuario es requerido.")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRespuesta
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class CambioClaveSolicitud
    {
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ClaveActual { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 6)]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$")]
        public string NuevaClave { get; set; } = string.Empty;
    }

    public class CambioClaveRespuesta
    {
        public string Rol { get; set; } = string.Empty;
    }
}
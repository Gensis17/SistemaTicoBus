using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL.Servicios;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/choferes")]
    public class ChoferesApiController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IEmailServicio _emailServicio;

        public ChoferesApiController(IConfiguration configuration, IEmailServicio emailServicio)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _emailServicio = emailServicio;
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<ChoferDto>>> Listar([FromQuery] string? busqueda)
        {
            // Listar choferes ahora se hace desde API, no desde el controller MVC.
            try
            {
                return Ok(ApiRespuesta<List<ChoferDto>>.Ok(ObtenerChoferes(busqueda)));
            }
            catch (SqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiRespuesta<List<ChoferDto>>.Error("No se pudieron cargar los choferes.")
                );
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiRespuesta<ChoferDto>>> Agregar(ChoferCrearSolicitud solicitud)
        {
            NormalizarChofer(solicitud);

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiRespuesta<ChoferDto>.Error(
                    "Verifique los datos del chofer. Todos los campos son requeridos y el correo debe ser válido."
                ));
            }

            try
            {
                if (ExisteChofer(solicitud.Identificacion))
                {
                    return BadRequest(ApiRespuesta<ChoferDto>.Error("Ya existe un chofer con esa identificación."));
                }

                if (ExisteCorreo(solicitud.Correo))
                {
                    return BadRequest(ApiRespuesta<ChoferDto>.Error("Ya existe un usuario registrado con ese correo."));
                }

                string nombreUsuario = GenerarNombreUsuario(solicitud.Nombre, solicitud.Apellidos);
                string claveGenerada = GenerarClaveAleatoria();

                CrearChoferConUsuario(solicitud, nombreUsuario, claveGenerada);

                bool correoEnviado = await IntentarEnviarClaveChoferAsync(
                    solicitud.Correo,
                    nombreUsuario,
                    claveGenerada
                );

                var dto = new ChoferDto
                {
                    Identificacion = solicitud.Identificacion,
                    Nombre = solicitud.Nombre,
                    Apellidos = solicitud.Apellidos,
                    Correo = solicitud.Correo,
                    NombreUsuario = nombreUsuario
                };

                string mensaje = correoEnviado
                    ? "Chofer registrado correctamente. La clave temporal fue enviada por Mailtrap."
                    : "Chofer registrado correctamente, pero no se pudo enviar el correo por Mailtrap.";

                return Ok(ApiRespuesta<ChoferDto>.Ok(dto, mensaje));
            }
            catch (SqlException ex)
            {
                return BadRequest(ApiRespuesta<ChoferDto>.Error(ObtenerMensajeSqlChofer(ex)));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiRespuesta<ChoferDto>.Error("Ocurrió un error al registrar el chofer.")
                );
            }
        }

        [HttpPut("{identificacionActual}")]
        public ActionResult<ApiRespuesta<ChoferDto>> Editar(string identificacionActual, ChoferEditarSolicitud solicitud)
        {
            identificacionActual = identificacionActual?.Trim() ?? string.Empty;
            NormalizarChofer(solicitud);

            if (string.IsNullOrWhiteSpace(identificacionActual))
            {
                return BadRequest(ApiRespuesta<ChoferDto>.Error("No se recibió la identificación actual del chofer."));
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiRespuesta<ChoferDto>.Error(
                    "Verifique los datos del chofer. Identificación, nombre y apellidos son requeridos."
                ));
            }

            try
            {
                ChoferDto? choferActual = ObtenerChoferPorIdentificacion(identificacionActual);

                if (choferActual == null)
                {
                    return NotFound(ApiRespuesta<ChoferDto>.Error("El chofer que intenta editar ya no existe."));
                }

                if (ExisteOtraIdentificacion(identificacionActual, solicitud.Identificacion))
                {
                    return BadRequest(ApiRespuesta<ChoferDto>.Error("Ya existe otro chofer con esa identificación."));
                }

                ActualizarChofer(identificacionActual, solicitud);

                var dto = new ChoferDto
                {
                    Identificacion = solicitud.Identificacion,
                    Nombre = solicitud.Nombre,
                    Apellidos = solicitud.Apellidos,
                    Correo = choferActual.Correo,
                    NombreUsuario = choferActual.NombreUsuario
                };

                return Ok(ApiRespuesta<ChoferDto>.Ok(dto, "Chofer actualizado correctamente."));
            }
            catch (SqlException ex)
            {
                return BadRequest(ApiRespuesta<ChoferDto>.Error(ObtenerMensajeSqlChofer(ex)));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiRespuesta<ChoferDto>.Error("Ocurrió un error al actualizar el chofer.")
                );
            }
        }

        [HttpDelete("{identificacion}")]
        public ActionResult<ApiRespuesta<object>> Eliminar(string identificacion)
        {
            // No lo pide la segunda entrega para módulo 2, pero se deja protegido por API Key porque la vista lo usa.
            identificacion = identificacion?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(identificacion))
            {
                return BadRequest(ApiRespuesta<object>.Error("No se recibió la identificación del chofer."));
            }

            try
            {
                if (ChoferTieneViajes(identificacion))
                {
                    return BadRequest(ApiRespuesta<object>.Error(
                        "No se puede eliminar el chofer porque tiene viajes registrados."
                    ));
                }

                EliminarChoferYUsuario(identificacion);

                return Ok(ApiRespuesta<object>.Ok(new { }, "Chofer eliminado correctamente."));
            }
            catch (SqlException ex)
            {
                return BadRequest(ApiRespuesta<object>.Error(ObtenerMensajeSqlChofer(ex)));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiRespuesta<object>.Error("Ocurrió un error al eliminar el chofer.")
                );
            }
        }

        private List<ChoferDto> ObtenerChoferes(string? busqueda)
        {
            List<ChoferDto> choferes = new List<ChoferDto>();

            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT 
                    c.Identificacion,
                    c.Nombre,
                    c.Apellidos,
                    u.Correo,
                    u.NombreUsuario
                FROM Choferes c
                INNER JOIN Usuarios u ON c.UsuarioId = u.Id
                WHERE 
                    @Busqueda IS NULL
                    OR @Busqueda = ''
                    OR c.Nombre LIKE '%' + @Busqueda + '%'
                    OR c.Apellidos LIKE '%' + @Busqueda + '%'
                ORDER BY c.Nombre, c.Apellidos";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@Busqueda", SqlDbType.VarChar, 100).Value =
                string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                choferes.Add(new ChoferDto
                {
                    Identificacion = reader["Identificacion"].ToString() ?? string.Empty,
                    Nombre = reader["Nombre"].ToString() ?? string.Empty,
                    Apellidos = reader["Apellidos"].ToString() ?? string.Empty,
                    Correo = reader["Correo"].ToString() ?? string.Empty,
                    NombreUsuario = reader["NombreUsuario"].ToString() ?? string.Empty
                });
            }

            return choferes;
        }

        private void CrearChoferConUsuario(ChoferCrearSolicitud solicitud, string nombreUsuario, string claveGenerada)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                int rolChoferId = ObtenerRolChoferId(connection, transaction);
                int usuarioId = CrearUsuarioChofer(connection, transaction, nombreUsuario, claveGenerada, solicitud.Correo, rolChoferId);
                CrearChofer(connection, transaction, solicitud, usuarioId);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private int ObtenerRolChoferId(SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT Id FROM Roles WHERE Nombre = 'Chofer'";

            using SqlCommand command = new SqlCommand(query, connection, transaction);
            object? result = command.ExecuteScalar();

            if (result == null)
            {
                throw new InvalidOperationException("No existe el rol Chofer en la base de datos.");
            }

            return Convert.ToInt32(result);
        }

        private int CrearUsuarioChofer(
            SqlConnection connection,
            SqlTransaction transaction,
            string nombreUsuario,
            string claveGenerada,
            string correo,
            int rolChoferId)
        {
            string query = @"
                INSERT INTO Usuarios
                    (NombreUsuario, Clave, Correo, RolId, BloqueadoHasta, IntentosFallidos)
                OUTPUT INSERTED.Id
                VALUES
                    (@NombreUsuario, @Clave, @Correo, @RolId, NULL, 0)";

            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;
            command.Parameters.Add("@Clave", SqlDbType.VarChar, 255).Value = claveGenerada;
            command.Parameters.Add("@Correo", SqlDbType.VarChar, 100).Value = correo;
            command.Parameters.Add("@RolId", SqlDbType.Int).Value = rolChoferId;

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private void CrearChofer(SqlConnection connection, SqlTransaction transaction, ChoferCrearSolicitud solicitud, int usuarioId)
        {
            string query = @"
                INSERT INTO Choferes
                    (Identificacion, Nombre, Apellidos, UsuarioId)
                VALUES
                    (@Identificacion, @Nombre, @Apellidos, @UsuarioId)";

            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = solicitud.Identificacion;
            command.Parameters.Add("@Nombre", SqlDbType.VarChar, 50).Value = solicitud.Nombre;
            command.Parameters.Add("@Apellidos", SqlDbType.VarChar, 50).Value = solicitud.Apellidos;
            command.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
            command.ExecuteNonQuery();
        }

        private void ActualizarChofer(string identificacionActual, ChoferEditarSolicitud solicitud)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Choferes
                SET
                    Identificacion = @NuevaIdentificacion,
                    Nombre = @Nombre,
                    Apellidos = @Apellidos
                WHERE Identificacion = @IdentificacionActual";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@NuevaIdentificacion", SqlDbType.VarChar, 30).Value = solicitud.Identificacion;
            command.Parameters.Add("@Nombre", SqlDbType.VarChar, 50).Value = solicitud.Nombre;
            command.Parameters.Add("@Apellidos", SqlDbType.VarChar, 50).Value = solicitud.Apellidos;
            command.Parameters.Add("@IdentificacionActual", SqlDbType.VarChar, 30).Value = identificacionActual;
            command.ExecuteNonQuery();
        }

        private void EliminarChoferYUsuario(string identificacion)
        {
            int usuarioId = ObtenerUsuarioIdDeChofer(identificacion);

            if (usuarioId == 0)
            {
                throw new InvalidOperationException("No se encontró el usuario asociado al chofer.");
            }

            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                using SqlCommand deleteChofer = new SqlCommand(
                    "DELETE FROM Choferes WHERE Identificacion = @Identificacion",
                    connection,
                    transaction
                );

                deleteChofer.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion;
                deleteChofer.ExecuteNonQuery();

                using SqlCommand deleteUsuario = new SqlCommand(
                    "DELETE FROM Usuarios WHERE Id = @UsuarioId",
                    connection,
                    transaction
                );

                deleteUsuario.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                deleteUsuario.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private bool ExisteChofer(string identificacion)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM Choferes WHERE Identificacion = @Identificacion",
                connection
            );

            command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion;
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool ExisteCorreo(string correo)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM Usuarios WHERE Correo = @Correo",
                connection
            );

            command.Parameters.Add("@Correo", SqlDbType.VarChar, 100).Value = correo;
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool ExisteNombreUsuario(string nombreUsuario)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario = @NombreUsuario",
                connection
            );

            command.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool ExisteOtraIdentificacion(string identificacionActual, string nuevaIdentificacion)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT COUNT(*)
                FROM Choferes
                WHERE Identificacion = @NuevaIdentificacion
                AND Identificacion <> @IdentificacionActual";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@NuevaIdentificacion", SqlDbType.VarChar, 30).Value = nuevaIdentificacion;
            command.Parameters.Add("@IdentificacionActual", SqlDbType.VarChar, 30).Value = identificacionActual;

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private bool ChoferTieneViajes(string identificacion)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM Viajes WHERE ChoferId = @ChoferId",
                connection
            );

            command.Parameters.Add("@ChoferId", SqlDbType.VarChar, 30).Value = identificacion;
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private ChoferDto? ObtenerChoferPorIdentificacion(string identificacion)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT 
                    c.Identificacion,
                    c.Nombre,
                    c.Apellidos,
                    u.Correo,
                    u.NombreUsuario
                FROM Choferes c
                INNER JOIN Usuarios u ON c.UsuarioId = u.Id
                WHERE c.Identificacion = @Identificacion";

            using SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion;

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new ChoferDto
            {
                Identificacion = reader["Identificacion"].ToString() ?? string.Empty,
                Nombre = reader["Nombre"].ToString() ?? string.Empty,
                Apellidos = reader["Apellidos"].ToString() ?? string.Empty,
                Correo = reader["Correo"].ToString() ?? string.Empty,
                NombreUsuario = reader["NombreUsuario"].ToString() ?? string.Empty
            };
        }

        private int ObtenerUsuarioIdDeChofer(string identificacion)
        {
            using SqlConnection connection = new SqlConnection(_connectionString);
            connection.Open();

            using SqlCommand command = new SqlCommand(
                "SELECT UsuarioId FROM Choferes WHERE Identificacion = @Identificacion",
                connection
            );

            command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion;

            object? result = command.ExecuteScalar();
            return result == null ? 0 : Convert.ToInt32(result);
        }

        private async Task<bool> IntentarEnviarClaveChoferAsync(string correo, string nombreUsuario, string claveGenerada)
        {
            string asunto = "Usuario Chofer creado — TicoBus";

            string cuerpo =
                $"Se creó su usuario de Chofer en TicoBus.\n\n" +
                $"Nombre de usuario: {nombreUsuario}\n" +
                $"Clave temporal: {claveGenerada}\n\n" +
                $"Por seguridad, cambie su clave al ingresar al sistema.";

            try
            {
                await _emailServicio.EnviarCorreoAsync(correo, asunto, cuerpo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerarNombreUsuario(string nombre, string apellidos)
        {
            string primerNombre = ObtenerPrimeraPalabra(nombre);
            string primerApellido = ObtenerPrimeraPalabra(apellidos);

            string nombreBase = $"chofer.{primerNombre}.{primerApellido}".ToLower();
            nombreBase = LimpiarTextoUsuario(nombreBase);

            string nombreUsuario = nombreBase;
            int contador = 1;

            while (ExisteNombreUsuario(nombreUsuario))
            {
                nombreUsuario = $"{nombreBase}{contador}";
                contador++;
            }

            return nombreUsuario;
        }

        private string GenerarClaveAleatoria()
        {
            const string letras = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz";
            const string numeros = "23456789";
            const string especiales = "*@#";
            const string todos = letras + numeros + especiales;

            StringBuilder clave = new StringBuilder();

            clave.Append(letras[RandomNumberGenerator.GetInt32(letras.Length)]);
            clave.Append(numeros[RandomNumberGenerator.GetInt32(numeros.Length)]);
            clave.Append(especiales[RandomNumberGenerator.GetInt32(especiales.Length)]);

            for (int i = 0; i < 7; i++)
            {
                clave.Append(todos[RandomNumberGenerator.GetInt32(todos.Length)]);
            }

            return new string(
                clave
                    .ToString()
                    .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                    .ToArray()
            );
        }

        private string ObtenerPrimeraPalabra(string texto)
        {
            string[] partes = texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length == 0 ? "usuario" : partes[0];
        }

        private string LimpiarTextoUsuario(string texto)
        {
            return texto
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("ñ", "n")
                .Replace("Á", "a")
                .Replace("É", "e")
                .Replace("Í", "i")
                .Replace("Ó", "o")
                .Replace("Ú", "u")
                .Replace("Ñ", "n");
        }

        private void NormalizarChofer(ChoferCrearSolicitud solicitud)
        {
            solicitud.Identificacion = solicitud.Identificacion?.Trim() ?? string.Empty;
            solicitud.Nombre = solicitud.Nombre?.Trim() ?? string.Empty;
            solicitud.Apellidos = solicitud.Apellidos?.Trim() ?? string.Empty;
            solicitud.Correo = solicitud.Correo?.Trim() ?? string.Empty;
        }

        private void NormalizarChofer(ChoferEditarSolicitud solicitud)
        {
            solicitud.Identificacion = solicitud.Identificacion?.Trim() ?? string.Empty;
            solicitud.Nombre = solicitud.Nombre?.Trim() ?? string.Empty;
            solicitud.Apellidos = solicitud.Apellidos?.Trim() ?? string.Empty;
        }

        private string ObtenerMensajeSqlChofer(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
            {
                if (error.Number == 2627 || error.Number == 2601)
                {
                    string mensaje = error.Message.ToLower();

                    if (mensaje.Contains("choferes") || mensaje.Contains("identificacion"))
                    {
                        return "Ya existe un chofer con esa identificación.";
                    }

                    if (mensaje.Contains("correo"))
                    {
                        return "Ya existe un usuario registrado con ese correo.";
                    }

                    if (mensaje.Contains("nombreusuario"))
                    {
                        return "Ya existe un usuario con el nombre generado. Intente con otro nombre o apellido.";
                    }

                    return "Ya existe un registro con esos datos. Verifique la identificación, usuario o correo.";
                }

                if (error.Number == 547)
                {
                    return "No se puede completar la operación porque el chofer tiene datos relacionados, como viajes registrados.";
                }
            }

            return "Ocurrió un error de base de datos al procesar el chofer.";
        }
    }

    public class ChoferDto
    {
        public string Identificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
    }

    public class ChoferCrearSolicitud
    {
        [Required(ErrorMessage = "La identificación es requerida.")]
        [StringLength(30)]
        public string Identificacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido.")]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos.")]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido.")]
        [EmailAddress]
        [StringLength(100)]
        public string Correo { get; set; } = string.Empty;
    }

    public class ChoferEditarSolicitud
    {
        [Required(ErrorMessage = "La identificación es requerida.")]
        [StringLength(30)]
        public string Identificacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido.")]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos.")]
        [StringLength(50)]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$")]
        public string Apellidos { get; set; } = string.Empty;
    }
}
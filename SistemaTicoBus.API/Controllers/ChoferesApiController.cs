using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL.Servicios;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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

        [HttpGet("dashboard/{usuarioId}")]
        public ActionResult<ApiRespuesta<ChoferDashboardViewModel>> ObtenerDashboardChofer(int usuarioId)
        {
            try
            {
                ChoferDashboardViewModel model = new ChoferDashboardViewModel
                {
                    Identificacion = "No disponible",
                    NombreCompleto = "Chofer",
                    Rol = "Chofer",
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
                    return Ok(ApiRespuesta<ChoferDashboardViewModel>.Ok(model, "No se encontró información del chofer."));
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

                return Ok(ApiRespuesta<ChoferDashboardViewModel>.Ok(model, "Dashboard del chofer cargado correctamente."));
            }
            catch (SqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiRespuesta<ChoferDashboardViewModel>.Error("No se pudieron cargar los viajes del chofer.")
                );
            }
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<ChoferDto>>> Listar([FromQuery] string? busqueda)
        {
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

            string? mensajeValidacion = ValidarChoferCrear(solicitud);

            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
            {
                return BadRequest(ApiRespuesta<ChoferDto>.Error(mensajeValidacion));
            }

            try
            {
                if (ExisteChofer(solicitud.Identificacion))
                {
                    return BadRequest(ApiRespuesta<ChoferDto>.Error("Ya existe un chofer con esa cédula."));
                }

                if (ExisteCorreo(solicitud.Correo))
                {
                    return BadRequest(ApiRespuesta<ChoferDto>.Error("Ya existe un usuario registrado con ese correo electrónico."));
                }

                string nombreUsuario = GenerarNombreUsuario(solicitud.Nombre, solicitud.Apellidos);
                string claveGenerada = GenerarClaveAleatoria();

                CrearChoferConUsuario(solicitud, nombreUsuario, claveGenerada);

                bool correoEnviado = await IntentarEnviarClaveChoferAsync(
                    solicitud.Correo,
                    nombreUsuario,
                    claveGenerada
                );

                ChoferDto dto = new ChoferDto
                {
                    Identificacion = solicitud.Identificacion,
                    Nombre = solicitud.Nombre,
                    Apellidos = solicitud.Apellidos,
                    Correo = solicitud.Correo,
                    NombreUsuario = nombreUsuario
                };

                string mensaje = correoEnviado
                    ? "Chofer registrado correctamente. La clave temporal fue enviada al correo indicado."
                    : "Chofer registrado correctamente, pero no se pudo enviar el correo con la clave temporal.";

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
                return BadRequest(ApiRespuesta<ChoferDto>.Error("No se recibió la cédula actual del chofer."));
            }

            string? mensajeValidacion = ValidarChoferEditar(solicitud);

            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
            {
                return BadRequest(ApiRespuesta<ChoferDto>.Error(mensajeValidacion));
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
                    return BadRequest(ApiRespuesta<ChoferDto>.Error("Ya existe otro chofer con esa cédula."));
                }

                ActualizarChofer(identificacionActual, solicitud);

                ChoferDto dto = new ChoferDto
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
            identificacion = identificacion?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(identificacion))
            {
                return BadRequest(ApiRespuesta<object>.Error("No se recibió la cédula del chofer."));
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
                "SELECT COUNT(*) FROM Usuarios WHERE LOWER(LTRIM(RTRIM(Correo))) = LOWER(@Correo)",
                connection
            );

            command.Parameters.Add("@Correo", SqlDbType.VarChar, 100).Value = correo.Trim();
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

            string nombreBase = $"chofer.{primerNombre}.{primerApellido}".ToLowerInvariant();
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
                .Replace("ü", "u")
                .Replace("Á", "a")
                .Replace("É", "e")
                .Replace("Í", "i")
                .Replace("Ó", "o")
                .Replace("Ú", "u")
                .Replace("Ñ", "n")
                .Replace("Ü", "u");
        }

        private void NormalizarChofer(ChoferCrearSolicitud solicitud)
        {
            solicitud.Identificacion = NormalizarTexto(solicitud.Identificacion);
            solicitud.Nombre = NormalizarTexto(solicitud.Nombre);
            solicitud.Apellidos = NormalizarTexto(solicitud.Apellidos);
            solicitud.Correo = NormalizarTexto(solicitud.Correo).ToLowerInvariant();
        }

        private void NormalizarChofer(ChoferEditarSolicitud solicitud)
        {
            solicitud.Identificacion = NormalizarTexto(solicitud.Identificacion);
            solicitud.Nombre = NormalizarTexto(solicitud.Nombre);
            solicitud.Apellidos = NormalizarTexto(solicitud.Apellidos);
        }

        private string NormalizarTexto(string? texto)
        {
            texto = texto?.Trim() ?? string.Empty;
            texto = Regex.Replace(texto, @"\s+", " ");
            return texto;
        }

        private string? ValidarChoferCrear(ChoferCrearSolicitud solicitud)
        {
            string? mensaje = ValidarDatosBasicosChofer(
                solicitud.Identificacion,
                solicitud.Nombre,
                solicitud.Apellidos
            );

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                return mensaje;
            }

            if (string.IsNullOrWhiteSpace(solicitud.Correo))
            {
                return "El correo electrónico es requerido.";
            }

            if (solicitud.Correo.Length > 100)
            {
                return "El correo electrónico no puede superar los 100 caracteres.";
            }

            EmailAddressAttribute validadorCorreo = new EmailAddressAttribute();

            if (!validadorCorreo.IsValid(solicitud.Correo))
            {
                return "Ingrese un correo electrónico válido.";
            }

            return null;
        }

        private string? ValidarChoferEditar(ChoferEditarSolicitud solicitud)
        {
            return ValidarDatosBasicosChofer(
                solicitud.Identificacion,
                solicitud.Nombre,
                solicitud.Apellidos
            );
        }

        private string? ValidarDatosBasicosChofer(string identificacion, string nombre, string apellidos)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
            {
                return "La cédula es requerida.";
            }

            if (!Regex.IsMatch(identificacion, @"^[0-9\-]+$"))
            {
                return "La cédula solo puede contener números y guiones.";
            }

            int cantidadDigitos = Regex.Replace(identificacion, @"\D", "").Length;

            if (cantidadDigitos < 6 || cantidadDigitos > 20)
            {
                return "La cédula debe tener entre 6 y 20 números.";
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return "El nombre es requerido.";
            }

            if (!Regex.IsMatch(nombre, @"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü]+(?: [A-Za-zÁÉÍÓÚáéíóúÑñÜü]+)*$"))
            {
                return "El nombre solo puede contener letras y espacios.";
            }

            if (nombre.Length < 2 || nombre.Length > 50)
            {
                return "El nombre debe tener entre 2 y 50 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(apellidos))
            {
                return "Los apellidos son requeridos.";
            }

            if (!Regex.IsMatch(apellidos, @"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü]+(?: [A-Za-zÁÉÍÓÚáéíóúÑñÜü]+)*$"))
            {
                return "Los apellidos solo pueden contener letras y espacios.";
            }

            if (apellidos.Length < 2 || apellidos.Length > 50)
            {
                return "Los apellidos deben tener entre 2 y 50 caracteres.";
            }

            return null;
        }

        private string ObtenerMensajeSqlChofer(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
            {
                if (error.Number == 2627 || error.Number == 2601)
                {
                    string mensaje = error.Message.ToLowerInvariant();

                    if (mensaje.Contains("choferes") || mensaje.Contains("identificacion"))
                    {
                        return "Ya existe un chofer con esa cédula.";
                    }

                    if (mensaje.Contains("correo"))
                    {
                        return "Ya existe un usuario registrado con ese correo electrónico.";
                    }

                    if (mensaje.Contains("nombreusuario"))
                    {
                        return "Ya existe un usuario con el nombre generado. Intente con otro nombre o apellido.";
                    }

                    return "Ya existe un registro con esos datos. Verifique la cédula, el usuario o el correo.";
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
        public string Identificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
    }

    public class ChoferEditarSolicitud
    {
        public string Identificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
    }

    public class ChoferDashboardViewModel
    {
        public string Identificacion { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public List<ViajeAsignadoDTO> Viajes { get; set; } = new List<ViajeAsignadoDTO>();
    }

    public class ViajeAsignadoDTO
    {
        public string IdViaje { get; set; } = string.Empty;
        public string Ruta { get; set; } = string.Empty;
        public string UnidadPlaca { get; set; } = string.Empty;
        public string HorarioSalida { get; set; } = string.Empty;
        public string Ocupacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.BL.Servicios;
using SistemaTicoBus.WEB.Models;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ChoferesController : Controller
    {
        private const string RolAdministrador = "Administrador";

        private readonly string _connectionString;
        private readonly IEmailServicio _emailServicio;

        public ChoferesController(IConfiguration configuration, IEmailServicio emailServicio)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            _emailServicio = emailServicio;
        }

        [HttpGet]
        public IActionResult Index(string? busqueda)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                List<ChoferViewModel> choferes = ObtenerChoferes(busqueda);
                ViewBag.Busqueda = busqueda;
                return View(choferes);
            }
            catch (SqlException)
            {
                TempData["MensajeError"] = "No se pudieron cargar los choferes. Verifique la conexión con la base de datos.";
                ViewBag.Busqueda = busqueda;
                return View(new List<ChoferViewModel>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChoferViewModel model)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            NormalizarChofer(model);

            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Verifique los datos del chofer. Todos los campos son requeridos y el correo debe tener formato válido.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (ExisteChofer(model.Identificacion))
                {
                    TempData["MensajeError"] = "Ya existe un chofer con esa identificación.";
                    return RedirectToAction(nameof(Index));
                }

                if (ExisteCorreo(model.Correo))
                {
                    TempData["MensajeError"] = "Ya existe un usuario registrado con ese correo.";
                    return RedirectToAction(nameof(Index));
                }

                string nombreUsuario = GenerarNombreUsuario(model.Nombre, model.Apellidos);
                string claveGenerada = GenerarClaveAleatoria();

                CrearChoferConUsuario(model, nombreUsuario, claveGenerada);

                bool correoEnviado = await IntentarEnviarClaveChoferAsync(model.Correo, nombreUsuario, claveGenerada);

                TempData["MensajeExito"] = correoEnviado
                    ? $"Chofer registrado correctamente. Usuario generado: {nombreUsuario}. La clave temporal fue enviada por Mailtrap."
                    : $"Chofer registrado correctamente. Usuario generado: {nombreUsuario}. No se pudo enviar el correo; revise Mailtrap.";

                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                TempData["MensajeError"] = ObtenerMensajeSqlChofer(ex);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error al registrar el chofer. Intente nuevamente.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, ChoferViewModel model)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["MensajeError"] = "No se recibió la identificación actual del chofer.";
                return RedirectToAction(nameof(Index));
            }

            NormalizarChofer(model);

            ModelState.Remove(nameof(ChoferViewModel.Correo));
            ModelState.Remove(nameof(ChoferViewModel.NombreUsuario));
            ModelState.Remove(nameof(ChoferViewModel.ClaveGenerada));

            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Verifique los datos del chofer. Identificación, nombre y apellidos son requeridos.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                ChoferViewModel? choferActual = ObtenerChoferPorIdentificacion(id);

                if (choferActual == null)
                {
                    TempData["MensajeError"] = "El chofer que intenta editar ya no existe.";
                    return RedirectToAction(nameof(Index));
                }

                if (ExisteOtraIdentificacion(id, model.Identificacion))
                {
                    TempData["MensajeError"] = "Ya existe otro chofer con esa identificación.";
                    return RedirectToAction(nameof(Index));
                }

                ActualizarChofer(id, model);

                TempData["MensajeExito"] = "Chofer actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                TempData["MensajeError"] = ObtenerMensajeSqlChofer(ex);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error al actualizar el chofer.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["MensajeError"] = "No se recibió la identificación del chofer a eliminar.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (ChoferTieneViajes(id))
                {
                    TempData["MensajeError"] = "No se puede eliminar el chofer porque tiene viajes registrados.";
                    return RedirectToAction(nameof(Index));
                }

                EliminarChoferYUsuario(id);

                TempData["MensajeExito"] = "Chofer eliminado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                TempData["MensajeError"] = ObtenerMensajeSqlChofer(ex);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error al eliminar el chofer.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool UsuarioEsAdministrador()
        {
            string? rol = HttpContext.Session.GetString("Rol");
            return rol == RolAdministrador;
        }

        private void NormalizarChofer(ChoferViewModel model)
        {
            model.Identificacion = model.Identificacion?.Trim() ?? string.Empty;
            model.Nombre = model.Nombre?.Trim() ?? string.Empty;
            model.Apellidos = model.Apellidos?.Trim() ?? string.Empty;
            model.Correo = model.Correo?.Trim() ?? string.Empty;
        }

        private List<ChoferViewModel> ObtenerChoferes(string? busqueda)
        {
            List<ChoferViewModel> choferes = new List<ChoferViewModel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
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

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Busqueda", SqlDbType.VarChar, 100).Value =
                        string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            choferes.Add(new ChoferViewModel
                            {
                                Identificacion = reader["Identificacion"].ToString() ?? string.Empty,
                                Nombre = reader["Nombre"].ToString() ?? string.Empty,
                                Apellidos = reader["Apellidos"].ToString() ?? string.Empty,
                                Correo = reader["Correo"].ToString() ?? string.Empty,
                                NombreUsuario = reader["NombreUsuario"].ToString() ?? string.Empty
                            });
                        }
                    }
                }
            }

            return choferes;
        }

        private void CrearChoferConUsuario(ChoferViewModel model, string nombreUsuario, string claveGenerada)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int rolChoferId = ObtenerRolChoferId(connection, transaction);
                        int usuarioId = CrearUsuarioChofer(connection, transaction, nombreUsuario, claveGenerada, model.Correo, rolChoferId);
                        CrearChofer(connection, transaction, model, usuarioId);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private void ActualizarChofer(string identificacionActual, ChoferViewModel model)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    UPDATE Choferes
                    SET 
                        Identificacion = @NuevaIdentificacion,
                        Nombre = @Nombre,
                        Apellidos = @Apellidos
                    WHERE Identificacion = @IdentificacionActual";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@NuevaIdentificacion", SqlDbType.VarChar, 30).Value = model.Identificacion.Trim();
                    command.Parameters.Add("@Nombre", SqlDbType.VarChar, 50).Value = model.Nombre.Trim();
                    command.Parameters.Add("@Apellidos", SqlDbType.VarChar, 50).Value = model.Apellidos.Trim();
                    command.Parameters.Add("@IdentificacionActual", SqlDbType.VarChar, 30).Value = identificacionActual.Trim();

                    command.ExecuteNonQuery();
                }
            }
        }

        private void EliminarChoferYUsuario(string identificacion)
        {
            int usuarioId = ObtenerUsuarioIdDeChofer(identificacion);

            if (usuarioId == 0)
            {
                throw new InvalidOperationException("No se encontró el usuario asociado al chofer.");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteChoferQuery = "DELETE FROM Choferes WHERE Identificacion = @Identificacion";

                        using (SqlCommand command = new SqlCommand(deleteChoferQuery, connection, transaction))
                        {
                            command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion.Trim();
                            command.ExecuteNonQuery();
                        }

                        string deleteUsuarioQuery = "DELETE FROM Usuarios WHERE Id = @UsuarioId";

                        using (SqlCommand command = new SqlCommand(deleteUsuarioQuery, connection, transaction))
                        {
                            command.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
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

        private bool ExisteChofer(string identificacion)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Choferes WHERE Identificacion = @Identificacion";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion.Trim();

                    int total = Convert.ToInt32(command.ExecuteScalar());
                    return total > 0;
                }
            }
        }

        private bool ExisteCorreo(string correo)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Usuarios WHERE Correo = @Correo";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Correo", SqlDbType.VarChar, 100).Value = correo.Trim();

                    int total = Convert.ToInt32(command.ExecuteScalar());
                    return total > 0;
                }
            }
        }

        private bool ExisteNombreUsuario(string nombreUsuario)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario = @NombreUsuario";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;

                    int total = Convert.ToInt32(command.ExecuteScalar());
                    return total > 0;
                }
            }
        }

        private bool ExisteOtraIdentificacion(string identificacionActual, string nuevaIdentificacion)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT COUNT(*) 
                    FROM Choferes 
                    WHERE Identificacion = @NuevaIdentificacion
                    AND Identificacion <> @IdentificacionActual";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@NuevaIdentificacion", SqlDbType.VarChar, 30).Value = nuevaIdentificacion.Trim();
                    command.Parameters.Add("@IdentificacionActual", SqlDbType.VarChar, 30).Value = identificacionActual.Trim();

                    int total = Convert.ToInt32(command.ExecuteScalar());
                    return total > 0;
                }
            }
        }

        private bool ChoferTieneViajes(string identificacion)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Viajes WHERE ChoferId = @ChoferId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@ChoferId", SqlDbType.VarChar, 30).Value = identificacion.Trim();

                    int total = Convert.ToInt32(command.ExecuteScalar());
                    return total > 0;
                }
            }
        }

        private ChoferViewModel? ObtenerChoferPorIdentificacion(string identificacion)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
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

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion.Trim();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        return new ChoferViewModel
                        {
                            Identificacion = reader["Identificacion"].ToString() ?? string.Empty,
                            Nombre = reader["Nombre"].ToString() ?? string.Empty,
                            Apellidos = reader["Apellidos"].ToString() ?? string.Empty,
                            Correo = reader["Correo"].ToString() ?? string.Empty,
                            NombreUsuario = reader["NombreUsuario"].ToString() ?? string.Empty
                        };
                    }
                }
            }
        }

        private int ObtenerRolChoferId(SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT Id FROM Roles WHERE Nombre = 'Chofer'";

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                object? result = command.ExecuteScalar();

                if (result == null)
                {
                    throw new InvalidOperationException("No existe el rol Chofer en la base de datos.");
                }

                return Convert.ToInt32(result);
            }
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

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;
                command.Parameters.Add("@Clave", SqlDbType.VarChar, 255).Value = claveGenerada;
                command.Parameters.Add("@Correo", SqlDbType.VarChar, 100).Value = correo.Trim();
                command.Parameters.Add("@RolId", SqlDbType.Int).Value = rolChoferId;

                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void CrearChofer(
            SqlConnection connection,
            SqlTransaction transaction,
            ChoferViewModel model,
            int usuarioId)
        {
            string query = @"
                INSERT INTO Choferes
                    (Identificacion, Nombre, Apellidos, UsuarioId)
                VALUES
                    (@Identificacion, @Nombre, @Apellidos, @UsuarioId)";

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = model.Identificacion.Trim();
                command.Parameters.Add("@Nombre", SqlDbType.VarChar, 50).Value = model.Nombre.Trim();
                command.Parameters.Add("@Apellidos", SqlDbType.VarChar, 50).Value = model.Apellidos.Trim();
                command.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;

                command.ExecuteNonQuery();
            }
        }

        private int ObtenerUsuarioIdDeChofer(string identificacion)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT UsuarioId FROM Choferes WHERE Identificacion = @Identificacion";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@Identificacion", SqlDbType.VarChar, 30).Value = identificacion.Trim();

                    object? result = command.ExecuteScalar();

                    if (result == null)
                    {
                        return 0;
                    }

                    return Convert.ToInt32(result);
                }
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

            if (partes.Length == 0)
            {
                return "usuario";
            }

            return partes[0];
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
}
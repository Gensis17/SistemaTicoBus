using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaTicoBus.DA.Repositorios
{
    public class PasajeroRepositorio
    {
        private readonly string _connectionString = "Server=localhost\\SQLEXPRESS;Database=TicoBusDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public void RegistrarPasajero(Pasajero pasajero)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            using SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                if (ExistePasajeroPorIdentificacion(conn, transaction, pasajero.Identificacion))
                {
                    throw new InvalidOperationException("Ya existe un pasajero con esa cédula.");
                }

                if (ExisteCorreo(conn, transaction, pasajero.Correo, usuarioIdExcluir: null))
                {
                    throw new InvalidOperationException("Ya existe un usuario registrado con ese correo electrónico.");
                }

                int rolPasajeroId = ObtenerRolPasajeroId(conn, transaction);
                string nombreUsuario = GenerarNombreUsuarioUnico(conn, transaction, pasajero.Nombre, pasajero.Apellidos);

                int nuevoUsuarioId = CrearUsuarioPasajero(
                    conn,
                    transaction,
                    nombreUsuario,
                    pasajero.Clave,
                    pasajero.Correo,
                    rolPasajeroId
                );

                CrearRegistroPasajero(conn, transaction, pasajero, nuevoUsuarioId);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<Pasajero> ObtenerPasajeros(string? buscarNombre = null)
        {
            List<Pasajero> lista = new List<Pasajero>();

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT 
                    p.Identificacion,
                    p.Nombre,
                    p.Apellidos,
                    u.Correo
                FROM Pasajeros p
                INNER JOIN Usuarios u ON p.UsuarioId = u.Id
                WHERE
                    @BuscarNombre IS NULL
                    OR @BuscarNombre = ''
                    OR p.Nombre LIKE '%' + @BuscarNombre + '%'
                    OR p.Apellidos LIKE '%' + @BuscarNombre + '%'
                ORDER BY p.Nombre, p.Apellidos";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue(
                "@BuscarNombre",
                string.IsNullOrWhiteSpace(buscarNombre) ? DBNull.Value : buscarNombre.Trim()
            );

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Pasajero
                {
                    Identificacion = reader["Identificacion"].ToString() ?? string.Empty,
                    Nombre = reader["Nombre"].ToString() ?? string.Empty,
                    Apellidos = reader["Apellidos"].ToString() ?? string.Empty,
                    Correo = reader["Correo"].ToString() ?? string.Empty,
                    Rol = "Pasajero"
                });
            }

            return lista;
        }

        public Pasajero? ObtenerPasajeroPorId(string identificacion)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT 
                    p.Identificacion,
                    p.Nombre,
                    p.Apellidos,
                    u.Correo
                FROM Pasajeros p
                INNER JOIN Usuarios u ON p.UsuarioId = u.Id
                WHERE p.Identificacion = @Id";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", identificacion);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new Pasajero
            {
                Identificacion = reader["Identificacion"].ToString() ?? string.Empty,
                Nombre = reader["Nombre"].ToString() ?? string.Empty,
                Apellidos = reader["Apellidos"].ToString() ?? string.Empty,
                Correo = reader["Correo"].ToString() ?? string.Empty,
                Rol = "Pasajero"
            };
        }

        public void EditarPasajero(Pasajero pasajero, string idOriginal)
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            using SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                int usuarioId = ObtenerUsuarioIdPasajero(conn, transaction, idOriginal);

                if (usuarioId <= 0)
                {
                    throw new InvalidOperationException("El pasajero que intenta editar ya no existe.");
                }

                if (ExisteOtraIdentificacion(conn, transaction, idOriginal, pasajero.Identificacion))
                {
                    throw new InvalidOperationException("Ya existe otro pasajero con esa cédula.");
                }

                if (ExisteCorreo(conn, transaction, pasajero.Correo, usuarioId))
                {
                    throw new InvalidOperationException("Ya existe otro usuario registrado con ese correo electrónico.");
                }

                ActualizarCorreoUsuario(conn, transaction, usuarioId, pasajero.Correo);
                ActualizarPasajero(conn, transaction, pasajero, idOriginal);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private int CrearUsuarioPasajero(
            SqlConnection conn,
            SqlTransaction transaction,
            string nombreUsuario,
            string clave,
            string correo,
            int rolPasajeroId)
        {
            string query = @"
                INSERT INTO Usuarios
                    (NombreUsuario, Clave, Correo, RolId, IntentosFallidos)
                OUTPUT INSERTED.Id
                VALUES
                    (@NombreUsuario, @Clave, @Correo, @RolId, 0)";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);
            cmd.Parameters.AddWithValue("@Clave", string.IsNullOrWhiteSpace(clave) ? "Pasa123*" : clave);
            cmd.Parameters.AddWithValue("@Correo", correo);
            cmd.Parameters.AddWithValue("@RolId", rolPasajeroId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private void CrearRegistroPasajero(
            SqlConnection conn,
            SqlTransaction transaction,
            Pasajero pasajero,
            int usuarioId)
        {
            string query = @"
                INSERT INTO Pasajeros
                    (Identificacion, Nombre, Apellidos, UsuarioId)
                VALUES
                    (@Id, @Nombre, @Apellidos, @UsuarioId)";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@Id", pasajero.Identificacion);
            cmd.Parameters.AddWithValue("@Nombre", pasajero.Nombre);
            cmd.Parameters.AddWithValue("@Apellidos", pasajero.Apellidos);
            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

            cmd.ExecuteNonQuery();
        }

        private void ActualizarCorreoUsuario(
            SqlConnection conn,
            SqlTransaction transaction,
            int usuarioId,
            string correo)
        {
            string query = @"
                UPDATE Usuarios
                SET Correo = @Correo
                WHERE Id = @UsuarioId";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@Correo", correo);
            cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

            cmd.ExecuteNonQuery();
        }

        private void ActualizarPasajero(
            SqlConnection conn,
            SqlTransaction transaction,
            Pasajero pasajero,
            string idOriginal)
        {
            string query = @"
                UPDATE Pasajeros
                SET 
                    Identificacion = @NuevaId,
                    Nombre = @Nombre,
                    Apellidos = @Apellidos
                WHERE Identificacion = @IdOriginal";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@NuevaId", pasajero.Identificacion);
            cmd.Parameters.AddWithValue("@Nombre", pasajero.Nombre);
            cmd.Parameters.AddWithValue("@Apellidos", pasajero.Apellidos);
            cmd.Parameters.AddWithValue("@IdOriginal", idOriginal);

            cmd.ExecuteNonQuery();
        }

        private int ObtenerRolPasajeroId(SqlConnection conn, SqlTransaction transaction)
        {
            string query = "SELECT TOP 1 Id FROM Roles WHERE Nombre = 'Pasajero'";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            object? resultado = cmd.ExecuteScalar();

            if (resultado == null)
            {
                throw new InvalidOperationException("No existe el rol Pasajero en la base de datos.");
            }

            return Convert.ToInt32(resultado);
        }

        private int ObtenerUsuarioIdPasajero(SqlConnection conn, SqlTransaction transaction, string identificacion)
        {
            string query = "SELECT UsuarioId FROM Pasajeros WHERE Identificacion = @Identificacion";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@Identificacion", identificacion);

            object? resultado = cmd.ExecuteScalar();
            return resultado == null ? 0 : Convert.ToInt32(resultado);
        }

        private bool ExistePasajeroPorIdentificacion(
            SqlConnection conn,
            SqlTransaction transaction,
            string identificacion)
        {
            string cedulaSinGuiones = QuitarGuiones(identificacion);

            string query = @"
                SELECT COUNT(*)
                FROM Pasajeros
                WHERE REPLACE(Identificacion, '-', '') = @CedulaSinGuiones";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@CedulaSinGuiones", cedulaSinGuiones);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private bool ExisteOtraIdentificacion(
            SqlConnection conn,
            SqlTransaction transaction,
            string idOriginal,
            string nuevaIdentificacion)
        {
            string cedulaSinGuiones = QuitarGuiones(nuevaIdentificacion);

            string query = @"
                SELECT COUNT(*)
                FROM Pasajeros
                WHERE REPLACE(Identificacion, '-', '') = @CedulaSinGuiones
                AND Identificacion <> @IdOriginal";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@CedulaSinGuiones", cedulaSinGuiones);
            cmd.Parameters.AddWithValue("@IdOriginal", idOriginal);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private bool ExisteCorreo(
            SqlConnection conn,
            SqlTransaction transaction,
            string correo,
            int? usuarioIdExcluir)
        {
            string query = @"
                SELECT COUNT(*)
                FROM Usuarios
                WHERE LOWER(LTRIM(RTRIM(Correo))) = LOWER(@Correo)
                AND (@UsuarioIdExcluir IS NULL OR Id <> @UsuarioIdExcluir)";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@Correo", correo.Trim());

            if (usuarioIdExcluir.HasValue)
            {
                cmd.Parameters.AddWithValue("@UsuarioIdExcluir", usuarioIdExcluir.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("@UsuarioIdExcluir", DBNull.Value);
            }

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private bool ExisteNombreUsuario(
            SqlConnection conn,
            SqlTransaction transaction,
            string nombreUsuario)
        {
            string query = "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario = @NombreUsuario";

            using SqlCommand cmd = new SqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private string GenerarNombreUsuarioUnico(
            SqlConnection conn,
            SqlTransaction transaction,
            string nombre,
            string apellidos)
        {
            string primerNombre = ObtenerPrimeraPalabra(nombre);
            string primerApellido = ObtenerPrimeraPalabra(apellidos);

            string baseUsuario = $"pasajero.{primerNombre}.{primerApellido}".ToLowerInvariant();
            baseUsuario = LimpiarParaNombreUsuario(baseUsuario);

            if (string.IsNullOrWhiteSpace(baseUsuario) || baseUsuario == "pasajero")
            {
                baseUsuario = "pasajero.usuario";
            }

            string nombreUsuario = baseUsuario;
            int contador = 1;

            while (ExisteNombreUsuario(conn, transaction, nombreUsuario))
            {
                nombreUsuario = $"{baseUsuario}{contador}";
                contador++;
            }

            return nombreUsuario;
        }

        private string ObtenerPrimeraPalabra(string texto)
        {
            string[] partes = texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length == 0 ? "usuario" : partes[0];
        }

        private string QuitarGuiones(string texto)
        {
            return (texto ?? string.Empty).Replace("-", "").Trim();
        }

        private string LimpiarParaNombreUsuario(string texto)
        {
            string textoSinTildes = QuitarTildes(texto);
            textoSinTildes = textoSinTildes.ToLowerInvariant();
            textoSinTildes = Regex.Replace(textoSinTildes, @"[^a-z0-9\.]", string.Empty);
            textoSinTildes = Regex.Replace(textoSinTildes, @"\.+", ".");
            textoSinTildes = textoSinTildes.Trim('.');

            return textoSinTildes;
        }

        private string QuitarTildes(string texto)
        {
            string normalizado = texto.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder();

            foreach (char c in normalizado)
            {
                UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(c);

                if (categoria != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
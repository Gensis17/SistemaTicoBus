using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.MODEL.Entidades;
using System.Data;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/rutas")]
    public class RutasApiController : ControllerBase
    {
        private readonly string _connectionString;

        public RutasApiController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<Ruta>>> Listar([FromQuery] string? buscar)
        {
            try
            {
                List<Ruta> rutas = new List<Ruta>();

                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT Id, Nombre, Origen, Destino, DuracionEstimada, PrecioBase
                    FROM Rutas
                    WHERE (@Buscar IS NULL
                           OR Nombre LIKE '%' + @Buscar + '%'
                           OR Destino LIKE '%' + @Buscar + '%')
                    ORDER BY Nombre";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.Add("@Buscar", SqlDbType.VarChar, 100).Value =
                    string.IsNullOrWhiteSpace(buscar) ? DBNull.Value : buscar.Trim();

                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    rutas.Add(new Ruta
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString() ?? string.Empty,
                        Origen = reader["Origen"].ToString() ?? string.Empty,
                        Destino = reader["Destino"].ToString() ?? string.Empty,
                        DuracionEstimada = (TimeSpan)reader["DuracionEstimada"],
                        PrecioBase = Convert.ToDecimal(reader["PrecioBase"])
                    });
                }

                return Ok(ApiRespuesta<List<Ruta>>.Ok(rutas));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<List<Ruta>>.Error("No se pudieron cargar las rutas."));
            }
        }

        [HttpGet("{id}")]
        public ActionResult<ApiRespuesta<Ruta>> ObtenerRuta(int id)
        {
            try
            {
                Ruta? ruta = null;

                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT Id, Nombre, Origen, Destino, DuracionEstimada, PrecioBase
                    FROM Rutas
                    WHERE Id = @Id";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                using SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    ruta = new Ruta
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString() ?? string.Empty,
                        Origen = reader["Origen"].ToString() ?? string.Empty,
                        Destino = reader["Destino"].ToString() ?? string.Empty,
                        DuracionEstimada = (TimeSpan)reader["DuracionEstimada"],
                        PrecioBase = Convert.ToDecimal(reader["PrecioBase"])
                    };
                }

                if (ruta == null)
                {
                    return NotFound(ApiRespuesta<Ruta>.Error("No se encontró la ruta."));
                }

                return Ok(ApiRespuesta<Ruta>.Ok(ruta));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Ruta>.Error("No se pudo cargar la ruta."));
            }
        }

        [HttpPost]
        public ActionResult<ApiRespuesta<Ruta>> Crear(Ruta ruta)
        {
            NormalizarRuta(ruta);

            if (string.IsNullOrWhiteSpace(ruta.Nombre) ||
                string.IsNullOrWhiteSpace(ruta.Origen) ||
                string.IsNullOrWhiteSpace(ruta.Destino))
            {
                return BadRequest(ApiRespuesta<Ruta>.Error("Nombre, origen y destino son requeridos."));
            }

            if (ruta.PrecioBase <= 0)
            {
                return BadRequest(ApiRespuesta<Ruta>.Error("El precio base debe ser mayor a cero."));
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    INSERT INTO Rutas (Nombre, Origen, Destino, DuracionEstimada, PrecioBase)
                    OUTPUT INSERTED.Id
                    VALUES (@Nombre, @Origen, @Destino, @DuracionEstimada, @PrecioBase)";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value = ruta.Nombre;
                command.Parameters.Add("@Origen", SqlDbType.VarChar, 100).Value = ruta.Origen;
                command.Parameters.Add("@Destino", SqlDbType.VarChar, 100).Value = ruta.Destino;
                command.Parameters.Add("@DuracionEstimada", SqlDbType.Time).Value = ruta.DuracionEstimada;
                command.Parameters.Add("@PrecioBase", SqlDbType.Decimal).Value = ruta.PrecioBase;

                ruta.Id = Convert.ToInt32(command.ExecuteScalar());

                return Ok(ApiRespuesta<Ruta>.Ok(ruta, "Ruta registrada correctamente."));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Ruta>.Error("Ocurrió un error al registrar la ruta."));
            }
        }

        [HttpPut("{id}")]
        public ActionResult<ApiRespuesta<Ruta>> Editar(int id, Ruta ruta)
        {
            NormalizarRuta(ruta);

            if (id <= 0)
            {
                return BadRequest(ApiRespuesta<Ruta>.Error("No se recibió el id de la ruta."));
            }

            if (string.IsNullOrWhiteSpace(ruta.Nombre) ||
                string.IsNullOrWhiteSpace(ruta.Origen) ||
                string.IsNullOrWhiteSpace(ruta.Destino))
            {
                return BadRequest(ApiRespuesta<Ruta>.Error("Nombre, origen y destino son requeridos."));
            }

            if (ruta.PrecioBase <= 0)
            {
                return BadRequest(ApiRespuesta<Ruta>.Error("El precio base debe ser mayor a cero."));
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    UPDATE Rutas 
                    SET Nombre = @Nombre,
                        Origen = @Origen,
                        Destino = @Destino,
                        DuracionEstimada = @DuracionEstimada,
                        PrecioBase = @PrecioBase
                    WHERE Id = @Id";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@Nombre", SqlDbType.VarChar, 100).Value = ruta.Nombre;
                command.Parameters.Add("@Origen", SqlDbType.VarChar, 100).Value = ruta.Origen;
                command.Parameters.Add("@Destino", SqlDbType.VarChar, 100).Value = ruta.Destino;
                command.Parameters.Add("@DuracionEstimada", SqlDbType.Time).Value = ruta.DuracionEstimada;
                command.Parameters.Add("@PrecioBase", SqlDbType.Decimal).Value = ruta.PrecioBase;

                int filas = command.ExecuteNonQuery();

                if (filas == 0)
                {
                    return NotFound(ApiRespuesta<Ruta>.Error("No se encontró la ruta que intenta editar."));
                }

                ruta.Id = id;

                return Ok(ApiRespuesta<Ruta>.Ok(ruta, "Ruta actualizada correctamente."));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Ruta>.Error("Ocurrió un error al actualizar la ruta."));
            }
        }

        [HttpDelete("{id}")]
        public ActionResult<ApiRespuesta<object>> Eliminar(int id)
        {
            if (id <= 0)
            {
                return BadRequest(ApiRespuesta<object>.Error("No se recibió el id de la ruta."));
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();

                string queryExiste = @"
                    SELECT COUNT(1)
                    FROM Rutas
                    WHERE Id = @Id";

                using SqlCommand commandExiste = new SqlCommand(queryExiste, connection);
                commandExiste.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                int existe = Convert.ToInt32(commandExiste.ExecuteScalar());

                if (existe == 0)
                {
                    return NotFound(ApiRespuesta<object>.Error("No se encontró la ruta que intenta eliminar."));
                }

                string queryViajes = @"
                    SELECT COUNT(1)
                    FROM Viajes
                    WHERE RutaId = @Id";

                using SqlCommand commandViajes = new SqlCommand(queryViajes, connection);
                commandViajes.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                int cantidadViajes = Convert.ToInt32(commandViajes.ExecuteScalar());

                if (cantidadViajes > 0)
                {
                    return BadRequest(ApiRespuesta<object>.Error(
                        "No se puede eliminar la ruta porque tiene viajes registrados."
                    ));
                }

                string queryEliminar = @"
                    DELETE FROM Rutas
                    WHERE Id = @Id";

                using SqlCommand commandEliminar = new SqlCommand(queryEliminar, connection);
                commandEliminar.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                int filas = commandEliminar.ExecuteNonQuery();

                if (filas == 0)
                {
                    return NotFound(ApiRespuesta<object>.Error("No se encontró la ruta que intenta eliminar."));
                }

                return Ok(ApiRespuesta<object>.Ok(new { }, "Ruta eliminada correctamente."));
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                return BadRequest(ApiRespuesta<object>.Error(
                    "No se puede eliminar la ruta porque está relacionada con otros registros del sistema."
                ));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<object>.Error("No se pudo eliminar la ruta."));
            }
        }

        private void NormalizarRuta(Ruta ruta)
        {
            ruta.Nombre = ruta.Nombre?.Trim() ?? string.Empty;
            ruta.Origen = ruta.Origen?.Trim() ?? string.Empty;
            ruta.Destino = ruta.Destino?.Trim() ?? string.Empty;
        }
    }
}
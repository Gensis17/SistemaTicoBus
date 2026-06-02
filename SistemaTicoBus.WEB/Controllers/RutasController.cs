using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using System.Data;

namespace SistemaTicoBus.WEB.Controllers
{
    public class RutasController : Controller
    {
        private readonly string _connectionString;

        public RutasController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        // 1. OBTENER DATOS PARA EDICIÓN 
        [HttpGet]
        public IActionResult ObtenerRuta(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Nombre, Origen, Destino, DuracionEstimada, PrecioBase FROM Rutas WHERE Id = @Id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            TempData["RutaEditarId"] = reader["Id"].ToString();
                            TempData["RutaEditarNombre"] = reader["Nombre"].ToString();
                            TempData["RutaEditarOrigen"] = reader["Origen"].ToString();
                            TempData["RutaEditarDestino"] = reader["Destino"].ToString();
                            TempData["RutaEditarDuracionEstimada"] = reader["DuracionEstimada"].ToString();
                            TempData["RutaEditarPrecioBase"] = reader["PrecioBase"].ToString();
                        }
                    }
                }
            }

            return RedirectToAction("AdminDashboard", "Account");
        }

        // 2. EDITAR RUTA 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Ruta model)
        {
            string? rol = HttpContext.Session.GetString("Rol");
            if (rol != "Administrador")
            {
                TempData["MensajeError"] = "Acceso denegado.";
                return RedirectToAction("AdminDashboard", "Account");
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"UPDATE Rutas SET Nombre = @Nombre, Origen = @Origen, Destino = @Destino, 
                                 DuracionEstimada = @DuracionEstimada, PrecioBase = @PrecioBase WHERE Id = @Id";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", model.Id);
                    command.Parameters.AddWithValue("@Nombre", model.Nombre);
                    command.Parameters.AddWithValue("@Origen", model.Origen);
                    command.Parameters.AddWithValue("@Destino", model.Destino);
                    command.Parameters.AddWithValue("@DuracionEstimada", model.DuracionEstimada);
                    command.Parameters.AddWithValue("@PrecioBase", model.PrecioBase);

                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("AdminDashboard", "Account");
        }

        // 3. CREAR RUTA 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(string nombre, string origen, string destino, string duracion, decimal precioBase)
        {
            if (HttpContext.Session.GetString("Rol") != "Administrador")
                return RedirectToAction("AdminDashboard", "Account");

            TimeSpan.TryParse(duracion, out TimeSpan duracionParsed);

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = "INSERT INTO Rutas (Nombre, Origen, Destino, DuracionEstimada, PrecioBase) VALUES (@N, @O, @D, @Dur, @P)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@N", nombre);
                    command.Parameters.AddWithValue("@O", origen);
                    command.Parameters.AddWithValue("@D", destino);
                    command.Parameters.AddWithValue("@Dur", duracionParsed);
                    command.Parameters.AddWithValue("@P", precioBase);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("AdminDashboard", "Account");
        }

        // 4. ELIMINAR RUTA 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarRuta(int id)
        {
            if (HttpContext.Session.GetString("Rol") == "Administrador")
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM Rutas WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
            }
            return RedirectToAction("AdminDashboard", "Account");
        }

        [HttpGet]
        public IActionResult ListadoRutas(string buscar)
        {
            List<Ruta> rutas = new List<Ruta>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"
        SELECT Id, Nombre, Origen, Destino, DuracionEstimada, PrecioBase
        FROM Rutas
        WHERE (@buscar IS NULL
               OR Nombre LIKE '%' + @buscar + '%'
               OR Destino LIKE '%' + @buscar + '%')
        ORDER BY Nombre";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@buscar",
                        string.IsNullOrWhiteSpace(buscar)
                            ? DBNull.Value
                            : buscar);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rutas.Add(new Ruta
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString(),
                                Origen = reader["Origen"].ToString(),
                                Destino = reader["Destino"].ToString(),
                                DuracionEstimada = (TimeSpan)reader["DuracionEstimada"],
                                PrecioBase = Convert.ToDecimal(reader["PrecioBase"])
                            });
                        }
                    }
                }
            }

            return View(rutas);
        }

        [HttpGet]
        public IActionResult Index(string buscar)
        {
            return RedirectToAction("ListadoRutas", new { buscar });
        }
    }
}

using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace SistemaTicoBus.DA.Repositorios
{
    public class ViajeRepositorio
    {

        private readonly string _connectionString;

        public ViajeRepositorio(string connectionString)
        {
            _connectionString = connectionString;
        }

        //Metodo para obtener todos los viajes
        public List<Viaje> ObtenerViajes()
        {
            var lista = new List<Viaje>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT v.NumeroViaje, v.RutaId, v.PlacaUnidad, v.ChoferId,
                                        v.FechaHoraSalida, v.FechaHoraLlegadaEstimada,
                                        v.Estado, v.MotivoCancelacion,
                                        r.Nombre AS NombreRuta,
                                        c.Nombre + ' ' + c.Apellidos AS NombreChofer
                                 FROM Viajes v
                                 INNER JOIN Rutas r ON v.RutaId = r.Id
                                 INNER JOIN Choferes c ON v.ChoferId = c.Identificacion";

                using (var cmd = new SqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(MapearViaje(reader));
                    }
                }

            }

            return lista;

        }

        //Metodo para obtener un viaje por su id
        public Viaje? ObtenerViajePorId(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT v.NumeroViaje, v.RutaId, v.PlacaUnidad, v.ChoferId,
                                        v.FechaHoraSalida, v.FechaHoraLlegadaEstimada,
                                        v.Estado, v.MotivoCancelacion,
                                        r.Nombre AS NombreRuta,
                                        c.Nombre + ' ' + c.Apellidos AS NombreChofer
                                 FROM Viajes v
                                 INNER JOIN Rutas r ON v.RutaId = r.Id
                                 INNER JOIN Choferes c ON v.ChoferId = c.Identificacion
                                 WHERE v.NumeroViaje = @Id";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapearViaje(reader);
                    }
                }
            }

            return null;
        }

        //Metodo para verificar si existe un conflicto de disponibilidad para una unidad o chofer en un rango de fechas
        public bool ExisteConflictoDeDisponibilidad(string placaUnidad, string choferId,
            DateTime fechaSalida, DateTime fechaLlegada, int? idViajeExcluir = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT COUNT(*) FROM Viajes
                                 WHERE Estado IN ('Programado', 'En Curso')
                                 AND (
                                     (PlacaUnidad = @Placa OR ChoferId = @ChoferId)
                                 )
                                 AND FechaHoraSalida < @FechaLlegada
                                 AND FechaHoraLlegadaEstimada > @FechaSalida
                                 AND (@IdExcluir IS NULL OR NumeroViaje <> @IdExcluir)";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Placa", placaUnidad);
                    cmd.Parameters.AddWithValue("@ChoferId", choferId);
                    cmd.Parameters.AddWithValue("@FechaSalida", fechaSalida);
                    cmd.Parameters.AddWithValue("@FechaLlegada", fechaLlegada);
                    cmd.Parameters.AddWithValue("@IdExcluir", (object?)idViajeExcluir ?? DBNull.Value);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        //Metodo para agregar un nuevo viaje
        public void AgregarViaje(Viaje viaje)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"INSERT INTO Viajes
                                 (RutaId, PlacaUnidad, ChoferId, FechaHoraSalida, FechaHoraLlegadaEstimada, Estado)
                                 VALUES
                                 (@RutaId, @Placa, @ChoferId, @Salida, @Llegada, 'Programado')";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@RutaId", viaje.IdRuta);
                    cmd.Parameters.AddWithValue("@Placa", viaje.PlacaUnidad);
                    cmd.Parameters.AddWithValue("@ChoferId", viaje.ChoferId);
                    cmd.Parameters.AddWithValue("@Salida", viaje.FechaHoraSalida);
                    cmd.Parameters.AddWithValue("@Llegada", viaje.FechaHoraLlegadaEstimada);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        //Metodo para editar un viaje existente
        public void EditarViaje(Viaje viaje)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"UPDATE Viajes
                                 SET RutaId = @RutaId,
                                     PlacaUnidad = @Placa,
                                     ChoferId = @ChoferId,
                                     FechaHoraSalida = @Salida,
                                     FechaHoraLlegadaEstimada = @Llegada
                                 WHERE NumeroViaje = @Id
                                 AND Estado = 'Programado'"; // se utiliza el AND para evitar editar viajes que ya están en curso o cancelados

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", viaje.IdViaje);
                    cmd.Parameters.AddWithValue("@RutaId", viaje.IdRuta);
                    cmd.Parameters.AddWithValue("@Placa", viaje.PlacaUnidad);
                    cmd.Parameters.AddWithValue("@ChoferId", viaje.ChoferId);
                    cmd.Parameters.AddWithValue("@Salida", viaje.FechaHoraSalida);
                    cmd.Parameters.AddWithValue("@Llegada", viaje.FechaHoraLlegadaEstimada);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        //Metodo para cancelar un viaje
        public void CancelarViaje(int idViaje, string motivo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"UPDATE Viajes
                                 SET Estado = 'Cancelado',
                                     MotivoCancelacion = @Motivo
                                 WHERE NumeroViaje = @Id
                                 AND Estado = 'Programado'"; //Tambien se usa el AND porque no se puede cancelar un viaje que ya esté en curso o que ya esté cancelado

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", idViaje);
                    cmd.Parameters.AddWithValue("@Motivo", motivo);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        //Metodo para iniciar un viaje
        public void IniciarViaje(int idViaje)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"UPDATE Viajes
                                 SET Estado = 'En Curso'
                                 WHERE NumeroViaje = @Id
                                 AND Estado = 'Programado'";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", idViaje);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        //Metodo para traer la lista de correos de los pasajeros que tienen reserva en un viaje específico
        public List<string> ObtenerCorreosPasajerosDelViaje(int idViaje)
        {
            var correos = new List<string>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                string query = @"SELECT u.Correo
                                 FROM Reservas r
                                 INNER JOIN Pasajeros p ON r.PasajeroId = p.Identificacion
                                 INNER JOIN Usuarios u ON p.UsuarioId = u.Id
                                 WHERE r.ViajeId = @IdViaje";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@IdViaje", idViaje);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            correos.Add(reader["Correo"].ToString()!);
                    }
                }
            }

            return correos;
        }

        //Metodo para convertir un SqlDataReader a un objeto Viaje
        private static Viaje MapearViaje(SqlDataReader reader)
        {
            return new Viaje
            {
                IdViaje = (int)reader["NumeroViaje"],
                IdRuta = (int)reader["RutaId"],
                PlacaUnidad = reader["PlacaUnidad"].ToString()!,
                ChoferId = reader["ChoferId"].ToString()!,
                FechaHoraSalida = (DateTime)reader["FechaHoraSalida"],
                FechaHoraLlegadaEstimada = (DateTime)reader["FechaHoraLlegadaEstimada"],
                Estado = reader["Estado"].ToString()!,
                MotivoCancelacion = reader["MotivoCancelacion"] as string,
                Ruta = new Ruta { Nombre = reader["NombreRuta"].ToString()! },
                Chofer = new Chofer { Nombre = reader["NombreChofer"].ToString()! }
            };
        }
    }
}

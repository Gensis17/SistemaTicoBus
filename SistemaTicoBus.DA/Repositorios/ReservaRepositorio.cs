using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.DA.Repositorios
{
    public class ReservaRepositorio
    {
        private readonly string _connectionString = "Server=localhost\\SQLEXPRESS;Database=TicoBusDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<Reserva> ObtenerReservasPorPasajero(string nombreUsuario)
        {
            var lista = new List<Reserva>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // JOIN para obtener el detalle de la reserva y el viaje
                string query = @"SELECT r.IdReserva, r.NumeroAsiento, r.FechaReserva, 
                                        v.FechaHoraSalida, ru.Nombre AS NombreRuta
                                 FROM Reservas r
                                 INNER JOIN Pasajeros p ON r.IdPasajero = p.Identificacion
                                 INNER JOIN Usuarios u ON p.UsuarioId = u.Id
                                 INNER JOIN Viajes v ON r.IdViaje = v.IdViaje
                                 INNER JOIN Rutas ru ON v.IdRuta = ru.IdRuta
                                 WHERE u.NombreUsuario = @NombreUsuario";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Reserva
                            {
                                IdReserva = (int)reader["IdReserva"],
                                NumeroAsiento = (int)reader["NumeroAsiento"],
                                Viaje = new Viaje
                                {
                                    FechaHoraSalida = (DateTime)reader["FechaHoraSalida"],
                                    Ruta = new Ruta { Nombre = reader["NombreRuta"].ToString() }
                                }
                            });
                        }
                    }
                }
            }
            return lista;
        }
    }
}

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
                string query = @"
                    SELECT
                        R.Id,
                        R.NumeroAsiento,
                        R.MontoPagado,
 
                        V.FechaHoraSalida,
                        V.FechaHoraLlegadaEstimada,
                        V.Estado,
                        V.PlacaUnidad,
                        V.ChoferId,
 
                        Ru.Nombre       AS RutaNombre,
                        Ru.PrecioBase,
 
                        -- Nombre completo del chofer
                        C.Nombre        AS ChoferNombre,
                        C.Apellidos     AS ChoferApellidos,
 
                        -- Datos de la unidad
                        Un.Modelo       AS UnidadModelo,
                        Un.CapacidadPasajeros
 
                    FROM Reservas R
                    INNER JOIN Pasajeros P   ON R.PasajeroId = P.Identificacion
                    INNER JOIN Usuarios  U   ON P.UsuarioId  = U.Id
                    INNER JOIN Viajes    V   ON R.ViajeId    = V.NumeroViaje
                    INNER JOIN Rutas     Ru  ON V.RutaId     = Ru.Id
                    INNER JOIN Choferes  C   ON V.ChoferId   = C.Identificacion
                    INNER JOIN Unidades  Un  ON V.PlacaUnidad = Un.Placa
                    WHERE U.NombreUsuario = @NombreUsuario
                    ORDER BY V.FechaHoraSalida DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Reserva
                            {
                                IdReserva = Convert.ToInt32(reader["Id"]),
                                NumeroAsiento = Convert.ToInt32(reader["NumeroAsiento"]),
                                MontoPagado = Convert.ToDecimal(reader["MontoPagado"]),

                                Viaje = new Viaje
                                {
                                    FechaHoraSalida = Convert.ToDateTime(reader["FechaHoraSalida"]),
                                    FechaHoraLlegadaEstimada = Convert.ToDateTime(reader["FechaHoraLlegadaEstimada"]),
                                    Estado = reader["Estado"].ToString(),
                                    PlacaUnidad = reader["PlacaUnidad"].ToString(),
                                    ChoferId = reader["ChoferId"].ToString(),

                                    Ruta = new Ruta
                                    {
                                        Nombre = reader["RutaNombre"].ToString(),
                                        PrecioBase = Convert.ToDecimal(reader["PrecioBase"])
                                    },

                                    Chofer = new Chofer
                                    {
                                        Identificacion = reader["ChoferId"].ToString(),
                                        Nombre = reader["ChoferNombre"].ToString(),
                                        Apellidos = reader["ChoferApellidos"].ToString()
                                    },

                                    Unidad = new Unidad
                                    {
                                        Placa = reader["PlacaUnidad"].ToString(),
                                        Modelo = reader["UnidadModelo"].ToString(),
                                        CapacidadPasajeros = Convert.ToInt32(reader["CapacidadPasajeros"])
                                    }
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

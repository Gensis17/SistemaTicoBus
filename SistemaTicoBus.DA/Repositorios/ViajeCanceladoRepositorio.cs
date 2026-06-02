using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.DA.Repositorios
{
    public class ViajeCanceladoRepositorio
    {
        private readonly string connectionString;

        public ViajeCanceladoRepositorio(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<ViajeCancelado> ListarViajesCancelados()
        {
            List<ViajeCancelado> lista = new List<ViajeCancelado>();

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        v.NumeroViaje,
                        r.Nombre AS Ruta,
                        v.PlacaUnidad,
                        c.Nombre + ' ' + c.Apellidos AS NombreChofer,
                        v.FechaHoraSalida,
                        v.FechaHoraLlegadaEstimada,
                        v.Estado,
                        v.MotivoCancelacion
                    FROM Viajes v
                    INNER JOIN Rutas r ON v.RutaId = r.Id
                    INNER JOIN Choferes c ON v.ChoferId = c.Identificacion
                    WHERE v.Estado = 'Cancelado'
                    ORDER BY v.FechaHoraSalida DESC";

                SqlCommand comando = new SqlCommand(query, conexion);

                conexion.Open();

                SqlDataReader reader = comando.ExecuteReader();

                while (reader.Read())
                {
                    ViajeCancelado viaje = new ViajeCancelado
                    {
                        NumeroViaje = Convert.ToInt32(reader["NumeroViaje"]),
                        Ruta = reader["Ruta"]?.ToString() ?? string.Empty,
                        PlacaUnidad = reader["PlacaUnidad"]?.ToString() ?? string.Empty,
                        NombreChofer = reader["NombreChofer"]?.ToString() ?? string.Empty,
                        FechaHoraSalida = Convert.ToDateTime(reader["FechaHoraSalida"]),
                        FechaHoraLlegadaEstimada = Convert.ToDateTime(reader["FechaHoraLlegadaEstimada"]),
                        Estado = reader["Estado"]?.ToString() ?? string.Empty,
                        MotivoCancelacion = reader["MotivoCancelacion"] == DBNull.Value
                            ? "Sin motivo registrado"
                            : reader["MotivoCancelacion"]?.ToString() ?? string.Empty
                    };

                    lista.Add(viaje);
                }
            }

            return lista;
        }

        public ViajeCancelado? ObtenerDetalleViajeCancelado(int numeroViaje)
        {
            ViajeCancelado? viaje = null;

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        v.NumeroViaje,
                        r.Nombre AS Ruta,
                        v.PlacaUnidad,
                        c.Nombre + ' ' + c.Apellidos AS NombreChofer,
                        v.FechaHoraSalida,
                        v.FechaHoraLlegadaEstimada,
                        v.Estado,
                        v.MotivoCancelacion
                    FROM Viajes v
                    INNER JOIN Rutas r ON v.RutaId = r.Id
                    INNER JOIN Choferes c ON v.ChoferId = c.Identificacion
                    WHERE v.NumeroViaje = @NumeroViaje
                    AND v.Estado = 'Cancelado'";

                SqlCommand comando = new SqlCommand(query, conexion);

                comando.Parameters.AddWithValue("@NumeroViaje", numeroViaje);

                conexion.Open();

                SqlDataReader reader = comando.ExecuteReader();

                if (reader.Read())
                {
                    viaje = new ViajeCancelado
                    {
                        NumeroViaje = Convert.ToInt32(reader["NumeroViaje"]),
                        Ruta = reader["Ruta"]?.ToString() ?? string.Empty,
                        PlacaUnidad = reader["PlacaUnidad"]?.ToString() ?? string.Empty,
                        NombreChofer = reader["NombreChofer"]?.ToString() ?? string.Empty,
                        FechaHoraSalida = Convert.ToDateTime(reader["FechaHoraSalida"]),
                        FechaHoraLlegadaEstimada = Convert.ToDateTime(reader["FechaHoraLlegadaEstimada"]),
                        Estado = reader["Estado"]?.ToString() ?? string.Empty,
                        MotivoCancelacion = reader["MotivoCancelacion"] == DBNull.Value
                            ? "Sin motivo registrado"
                            : reader["MotivoCancelacion"]?.ToString() ?? string.Empty
                    };
                }
            }

            return viaje;
        }
    }
}

using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.DA.Repositorios
{
    public class UnidadRepositorio
    {
        private readonly string _connectionString =
           "Server=localhost\\SQLEXPRESS;Database=TicoBusDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // LISTAR
        public List<Unidad> ObtenerUnidades()
        {
            List<Unidad> lista = new List<Unidad>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"SELECT Placa,
                                        Modelo,
                                        AnioFabricacion,
                                        CapacidadPasajeros
                                 FROM Unidades";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Unidad
                            {
                                Placa = reader["Placa"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                AnioFabricacion = (int)reader["AnioFabricacion"],
                                CapacidadPasajeros = (int)reader["CapacidadPasajeros"]
                            });
                        }
                    }
                }
            }

            return lista;
        }

        // OBTENER POR PLACA
        public Unidad ObtenerUnidadPorPlaca(string placa)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"SELECT *
                                 FROM Unidades
                                 WHERE Placa = @Placa";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Placa", placa);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Unidad
                            {
                                Placa = reader["Placa"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                AnioFabricacion = (int)reader["AnioFabricacion"],
                                CapacidadPasajeros = (int)reader["CapacidadPasajeros"]
                            };
                        }
                    }
                }
            }

            return null;
        }

        // VALIDAR PLACA
        public bool ExistePlaca(string placa)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"SELECT COUNT(*)
                                 FROM Unidades
                                 WHERE Placa = @Placa";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Placa", placa);

                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        // AGREGAR
        public void AgregarUnidad(Unidad unidad)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"INSERT INTO Unidades
                                 (Placa, Modelo, AnioFabricacion, CapacidadPasajeros)
                                 VALUES
                                 (@Placa, @Modelo, @Anio, @Capacidad)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Placa", unidad.Placa);
                    cmd.Parameters.AddWithValue("@Modelo", unidad.Modelo);
                    cmd.Parameters.AddWithValue("@Anio", unidad.AnioFabricacion);
                    cmd.Parameters.AddWithValue("@Capacidad", unidad.CapacidadPasajeros);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // EDITAR
        public void EditarUnidad(Unidad unidad, string placaOriginal)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"UPDATE Unidades
                                 SET Placa = @NuevaPlaca,
                                     Modelo = @Modelo,
                                     AnioFabricacion = @Anio,
                                     CapacidadPasajeros = @Capacidad
                                 WHERE Placa = @PlacaOriginal";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NuevaPlaca", unidad.Placa);
                    cmd.Parameters.AddWithValue("@Modelo", unidad.Modelo);
                    cmd.Parameters.AddWithValue("@Anio", unidad.AnioFabricacion);
                    cmd.Parameters.AddWithValue("@Capacidad", unidad.CapacidadPasajeros);
                    cmd.Parameters.AddWithValue("@PlacaOriginal", placaOriginal);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

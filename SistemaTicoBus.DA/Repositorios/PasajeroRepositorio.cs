using Microsoft.Data.SqlClient;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.DA.Repositorios
{
    public class PasajeroRepositorio
    {
        private readonly string _connectionString = "Server=localhost\\SQLEXPRESS;Database=TicoBusDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // REGISTRAR PASAJERO
        public void RegistrarPasajero(Pasajero pasajero)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string correoDestino = !string.IsNullOrEmpty(pasajero.Correo)
                                                ? pasajero.Correo
                                                : $"pasajero_{pasajero.Identificacion}@ticobus.com";

                        string queryUsuario = @"INSERT INTO Usuarios (NombreUsuario, Clave, Correo, RolId, IntentosFallidos) 
                                                OUTPUT INSERTED.Id
                                                VALUES (@NombreUsuario, @Clave, @Correo, @RolId, @IntentosFallidos)";

                        int nuevoUsuarioId = 0;

                        using (SqlCommand cmdUser = new SqlCommand(queryUsuario, conn, transaction))
                        {
                            string usuarioFormateado = $"pasajero.{pasajero.Nombre.ToLower().Replace(" ", "")}";

                            cmdUser.Parameters.AddWithValue("@NombreUsuario", usuarioFormateado);
                            cmdUser.Parameters.AddWithValue("@Clave", "Pasa123*");
                            cmdUser.Parameters.AddWithValue("@Correo", correoDestino);
                            cmdUser.Parameters.AddWithValue("@RolId", 3);
                            cmdUser.Parameters.AddWithValue("@IntentosFallidos", 0);

                            nuevoUsuarioId = (int)cmdUser.ExecuteScalar();
                        }

                        string queryPasajero = @"INSERT INTO Pasajeros (Identificacion, Nombre, Apellidos, UsuarioId) 
                                                 VALUES (@Id, @Nombre, @Apellidos, @UsuarioId)";

                        using (SqlCommand cmdPasajero = new SqlCommand(queryPasajero, conn, transaction))
                        {
                            cmdPasajero.Parameters.AddWithValue("@Id", pasajero.Identificacion);
                            cmdPasajero.Parameters.AddWithValue("@Nombre", pasajero.Nombre);
                            cmdPasajero.Parameters.AddWithValue("@Apellidos", pasajero.Apellidos);
                            cmdPasajero.Parameters.AddWithValue("@UsuarioId", nuevoUsuarioId);

                            cmdPasajero.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();

                        if (ex.Number == 2627 || ex.Number == 2601)
                        {
                            if (ex.Message.Contains("Correo") || ex.Message.Contains("correo") || ex.Message.Contains("UQ_Usuarios"))
                                throw new InvalidOperationException("El correo electrónico ingresado ya está registrado en el sistema.");

                            if (ex.Message.Contains("Identificacion") || ex.Message.Contains("Pasajeros") || ex.Message.Contains("PK_"))
                                throw new InvalidOperationException("La cédula ingresada ya está registrada en el sistema.");

                            throw new InvalidOperationException("Ya existe un pasajero registrado con esa cédula o correo electrónico.");
                        }

                        throw; 
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // OBTENER LISTADO
        public List<Pasajero> ObtenerPasajeros(string buscarNombre = null)
        {
            var lista = new List<Pasajero>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"SELECT p.Identificacion, p.Nombre, p.Apellidos, u.Correo 
                                 FROM Pasajeros p
                                 INNER JOIN Usuarios u ON p.UsuarioId = u.Id
                                 WHERE 1=1";

                if (!string.IsNullOrEmpty(buscarNombre))
                    query += " AND p.Nombre LIKE @Buscar";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(buscarNombre))
                        cmd.Parameters.AddWithValue("@Buscar", "%" + buscarNombre + "%");

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new Pasajero
                            {
                                Identificacion = reader["Identificacion"].ToString(),
                                Nombre = reader["Nombre"].ToString(),
                                Apellidos = reader["Apellidos"].ToString(),
                                Correo = reader["Correo"].ToString()
                            });
                        }
                    }
                }
            }
            return lista;
        }

        //OBTENER POR ID
        public Pasajero ObtenerPasajeroPorId(string identificacion)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"SELECT p.Identificacion, p.Nombre, p.Apellidos, u.Correo 
                                 FROM Pasajeros p
                                 INNER JOIN Usuarios u ON p.UsuarioId = u.Id 
                                 WHERE p.Identificacion = @Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", identificacion);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Pasajero
                            {
                                Identificacion = reader["Identificacion"].ToString(),
                                Nombre = reader["Nombre"].ToString(),
                                Apellidos = reader["Apellidos"].ToString(),
                                Correo = reader["Correo"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        // EDITAR PASAJERO
        public void EditarPasajero(Pasajero pasajero, string idOriginal)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string queryUser = @"UPDATE Usuarios 
                                             SET Correo = @Correo 
                                             WHERE Id = (SELECT UsuarioId FROM Pasajeros WHERE Identificacion = @IdOriginal)";

                        using (SqlCommand cmdUser = new SqlCommand(queryUser, conn, transaction))
                        {
                            cmdUser.Parameters.AddWithValue("@Correo", !string.IsNullOrEmpty(pasajero.Correo) ? pasajero.Correo : "");
                            cmdUser.Parameters.AddWithValue("@IdOriginal", idOriginal);
                            cmdUser.ExecuteNonQuery();
                        }

                        string queryPasajero = @"UPDATE Pasajeros 
                                                 SET Identificacion = @NuevaId, Nombre = @Nombre, Apellidos = @Apellidos 
                                                 WHERE Identificacion = @IdOriginal";

                        using (SqlCommand cmdPasajero = new SqlCommand(queryPasajero, conn, transaction))
                        {
                            cmdPasajero.Parameters.AddWithValue("@NuevaId", pasajero.Identificacion);
                            cmdPasajero.Parameters.AddWithValue("@Nombre", pasajero.Nombre);
                            cmdPasajero.Parameters.AddWithValue("@Apellidos", pasajero.Apellidos);
                            cmdPasajero.Parameters.AddWithValue("@IdOriginal", idOriginal);
                            cmdPasajero.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();

                        if (ex.Number == 2627 || ex.Number == 2601)
                        {
                            if (ex.Message.Contains("Correo") || ex.Message.Contains("UQ_Usuarios"))
                                throw new InvalidOperationException("El correo electrónico ya está en uso por otro pasajero.");

                            if (ex.Message.Contains("Identificacion") || ex.Message.Contains("PK_"))
                                throw new InvalidOperationException("La cédula ingresada ya está registrada para otro pasajero.");

                            throw new InvalidOperationException("Ya existe un pasajero con esa cédula o correo electrónico.");
                        }

                        throw;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}

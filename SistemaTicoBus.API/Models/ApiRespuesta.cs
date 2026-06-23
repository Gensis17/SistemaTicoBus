namespace SistemaTicoBus.API.Models
{
    public class ApiRespuesta<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Datos { get; set; }

        public static ApiRespuesta<T> Ok(T datos, string mensaje = "Operación realizada correctamente.")
        {
            return new ApiRespuesta<T>
            {
                Exito = true,
                Mensaje = mensaje,
                Datos = datos
            };
        }

        public static ApiRespuesta<T> Error(string mensaje)
        {
            return new ApiRespuesta<T>
            {
                Exito = false,
                Mensaje = mensaje,
                Datos = default
            };
        }
    }
}
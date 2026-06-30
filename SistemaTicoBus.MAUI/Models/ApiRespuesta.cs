namespace SistemaTicoBus.MAUI.Models
{
    public class ApiRespuesta<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Datos { get; set; }
    }

    public class LoginRespuesta
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
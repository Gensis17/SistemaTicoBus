using SistemaTicoBus.WEB.Models;

namespace SistemaTicoBus.WEB.Services.Api
{
    public interface ITicoBusApiClient
    {
        Task<ApiResultado<LoginApiDatos>> LoginAsync(LoginViewModel model);
        Task<ApiResultado<CambioClaveApiDatos>> CambiarClaveAsync(ChangePasswordViewModel model);

        Task<ApiResultado<List<ChoferViewModel>>> ObtenerChoferesAsync(string? busqueda);
        Task<ApiResultado<ChoferViewModel>> CrearChoferAsync(ChoferViewModel model);
        Task<ApiResultado<ChoferViewModel>> EditarChoferAsync(string identificacionActual, ChoferViewModel model);
        Task<ApiResultado<object>> EliminarChoferAsync(string identificacion);
    }

    public class ApiResultado<T>
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Datos { get; set; }
    }

    public class LoginApiDatos
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class CambioClaveApiDatos
    {
        public string Rol { get; set; } = string.Empty;
    }
}
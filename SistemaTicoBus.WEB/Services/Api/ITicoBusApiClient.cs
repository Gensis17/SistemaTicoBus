using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;
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

        Task<ApiResultado<List<Unidad>>> ObtenerUnidadesAsync();
        Task<ApiResultado<Unidad>> CrearUnidadAsync(Unidad model);
        Task<ApiResultado<Unidad>> EditarUnidadAsync(string placaOriginal, Unidad model);

        Task<ApiResultado<List<ViajeCancelado>>> ObtenerViajesCanceladosAsync();
        Task<ApiResultado<ViajeCancelado>> ObtenerDetalleViajeCanceladoAsync(int id);

        Task<ApiResultado<List<Pasajero>>> ObtenerPasajerosAsync(string? buscarNombre);
        Task<ApiResultado<Pasajero>> CrearPasajeroAsync(Pasajero model);
        Task<ApiResultado<Pasajero>> EditarPasajeroAsync(string idOriginal, Pasajero model);
        Task<ApiResultado<Pasajero>> ObtenerPasajeroAsync(string identificacion);

        Task<ApiResultado<List<Ruta>>> ObtenerRutasAsync(string? buscar);
        Task<ApiResultado<Ruta>> ObtenerRutaAsync(int id);
        Task<ApiResultado<Ruta>> CrearRutaAsync(Ruta model);
        Task<ApiResultado<Ruta>> EditarRutaAsync(int id, Ruta model);
        Task<ApiResultado<object>> EliminarRutaAsync(int id);

        Task<ApiResultado<List<Viaje>>> ObtenerViajesAsync(string? filtro);
        Task<ApiResultado<Viaje>> ObtenerViajeAsync(int id);
        Task<ApiResultado<Viaje>> CrearViajeAsync(Viaje model);
        Task<ApiResultado<Viaje>> EditarViajeAsync(int id, Viaje model);
        Task<ApiResultado<object>> CancelarViajeAsync(int id, string motivo);
        Task<ApiResultado<object>> IniciarViajeAsync(int id);

        Task<ApiResultado<List<Viaje>>> ObtenerViajesEnCursoAsync();
        Task<ApiResultado<Viaje>> ObtenerDetalleViajeEnCursoAsync(int id);
        Task<ApiResultado<List<PasajeroCatalogoDTO>>> ObtenerCatalogoPasajerosAsync();
        Task<ApiResultado<object>> ReservarViajeEnCursoAsync(int idViaje, string idPasajero, int numeroAsiento);
        Task<ApiResultado<object>> CancelarReservaViajeEnCursoAsync(int idReserva);
        Task<ApiResultado<object>> FinalizarViajeEnCursoAsync(int idViaje);
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
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SistemaTicoBus.WEB.Services.Api
{
    public class TicoBusApiClient : ITicoBusApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public TicoBusApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ApiResultado<LoginApiDatos>> LoginAsync(LoginViewModel model)
        {
            // Login ahora viaja por API con API Key.
            return await PostAsync<LoginViewModel, LoginApiDatos>("api/auth/login", model);
        }

        public async Task<ApiResultado<CambioClaveApiDatos>> CambiarClaveAsync(ChangePasswordViewModel model)
        {
            // Cambio de clave ahora viaja por API con API Key.
            return await PostAsync<ChangePasswordViewModel, CambioClaveApiDatos>("api/auth/cambiar-clave", model);
        }

        public async Task<ApiResultado<List<ChoferViewModel>>> ObtenerChoferesAsync(string? busqueda)
        {
            string url = "api/choferes";

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                url += $"?busqueda={Uri.EscapeDataString(busqueda)}";
            }

            return await GetAsync<List<ChoferViewModel>>(url);
        }

        public async Task<ApiResultado<ChoferViewModel>> CrearChoferAsync(ChoferViewModel model)
        {
            return await PostAsync<ChoferViewModel, ChoferViewModel>("api/choferes", model);
        }

        public async Task<ApiResultado<ChoferViewModel>> EditarChoferAsync(string identificacionActual, ChoferViewModel model)
        {
            string url = $"api/choferes/{Uri.EscapeDataString(identificacionActual)}";
            return await PutAsync<ChoferViewModel, ChoferViewModel>(url, model);
        }

        public async Task<ApiResultado<object>> EliminarChoferAsync(string identificacion)
        {
            string url = $"api/choferes/{Uri.EscapeDataString(identificacion)}";

            try
            {
                HttpResponseMessage response = await _httpClient.DeleteAsync(url);
                return await LeerRespuestaAsync<object>(response);
            }
            catch
            {
                return ApiError<object>("No se pudo conectar con la API.");
            }
        }

        public async Task<ApiResultado<List<Unidad>>> ObtenerUnidadesAsync()
        {
            return await GetAsync<List<Unidad>>("api/unidades");
        }

        public async Task<ApiResultado<Unidad>> CrearUnidadAsync(Unidad model)
        {
            return await PostAsync<Unidad, Unidad>("api/unidades", model);
        }

        public async Task<ApiResultado<Unidad>> EditarUnidadAsync(string placaOriginal, Unidad model)
        {
            string url = $"api/unidades/{Uri.EscapeDataString(placaOriginal)}";
            return await PutAsync<Unidad, Unidad>(url, model);
        }

        public async Task<ApiResultado<List<ViajeCancelado>>> ObtenerViajesCanceladosAsync()
        {
            return await GetAsync<List<ViajeCancelado>>("api/viajescancelados");
        }

        public async Task<ApiResultado<ViajeCancelado>> ObtenerDetalleViajeCanceladoAsync(int id)
        {
            return await GetAsync<ViajeCancelado>($"api/viajescancelados/{id}");
        }

        // Pasajeros
        public async Task<ApiResultado<List<Pasajero>>> ObtenerPasajerosAsync(string? buscarNombre)
        {
            string url = "api/pasajeros";

            if (!string.IsNullOrWhiteSpace(buscarNombre))
            {
                url += $"?buscarNombre={Uri.EscapeDataString(buscarNombre)}";
            }

            return await GetAsync<List<Pasajero>>(url);
        }

        public async Task<ApiResultado<Pasajero>> CrearPasajeroAsync(Pasajero model)
        {
            return await PostAsync<Pasajero, Pasajero>("api/pasajeros", model);
        }

        public async Task<ApiResultado<Pasajero>> EditarPasajeroAsync(string idOriginal, Pasajero model)
        {
            string url = $"api/pasajeros/{Uri.EscapeDataString(idOriginal)}";
            return await PutAsync<Pasajero, Pasajero>(url, model);
        }
        public async Task<ApiResultado<Pasajero>> ObtenerPasajeroAsync(string identificacion)
        {
            return await GetAsync<Pasajero>(
                $"api/pasajeros/{Uri.EscapeDataString(identificacion)}");
        }

        // Rutas
        public async Task<ApiResultado<List<Ruta>>> ObtenerRutasAsync(string? buscar)
        {
            string url = "api/rutas";

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                url += $"?buscar={Uri.EscapeDataString(buscar)}";
            }

            return await GetAsync<List<Ruta>>(url);
        }

        public async Task<ApiResultado<Ruta>> ObtenerRutaAsync(int id)
        {
            return await GetAsync<Ruta>($"api/rutas/{id}");
        }

        public async Task<ApiResultado<Ruta>> CrearRutaAsync(Ruta model)
        {
            return await PostAsync<Ruta, Ruta>("api/rutas", model);
        }

        public async Task<ApiResultado<Ruta>> EditarRutaAsync(int id, Ruta model)
        {
            return await PutAsync<Ruta, Ruta>($"api/rutas/{id}", model);
        }
        public async Task<ApiResultado<object>> EliminarRutaAsync(int id)
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.DeleteAsync($"api/rutas/{id}");

                return await LeerRespuestaAsync<object>(response);
            }
            catch
            {
                return ApiError<object>("No se pudo conectar con la API.");
            }
        }
        public async Task<ApiResultado<List<Viaje>>> ObtenerViajesAsync(string? filtro)
        {
            string url = "api/viajes";

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                url += $"?filtro={Uri.EscapeDataString(filtro)}";
            }

            return await GetAsync<List<Viaje>>(url);
        }

        public async Task<ApiResultado<Viaje>> ObtenerViajeAsync(int id)
        {
            return await GetAsync<Viaje>($"api/viajes/{id}");
        }

        public async Task<ApiResultado<Viaje>> CrearViajeAsync(Viaje model)
        {
            return await PostAsync<Viaje, Viaje>("api/viajes", model);
        }

        public async Task<ApiResultado<Viaje>> EditarViajeAsync(int id, Viaje model)
        {
            return await PutAsync<Viaje, Viaje>($"api/viajes/{id}", model);
        }

        public async Task<ApiResultado<object>> CancelarViajeAsync(int id, string motivo)
        {
            var request = new
            {
                motivo = motivo
            };

            return await PutAsync<object, object>($"api/viajes/{id}/cancelar", request);
        }

        public async Task<ApiResultado<object>> IniciarViajeAsync(int id)
        {
            var request = new { };

            return await PutAsync<object, object>($"api/viajes/{id}/iniciar", request);
        }

        public async Task<ApiResultado<List<Viaje>>> ObtenerViajesEnCursoAsync()
        {
            return await GetAsync<List<Viaje>>("api/viajesencurso");
        }

        public async Task<ApiResultado<Viaje>> ObtenerDetalleViajeEnCursoAsync(int id)
        {
            return await GetAsync<Viaje>($"api/viajesencurso/{id}");
        }

        public async Task<ApiResultado<List<PasajeroCatalogoDTO>>> ObtenerCatalogoPasajerosAsync()
        {
            return await GetAsync<List<PasajeroCatalogoDTO>>("api/viajesencurso/pasajeros");
        }

        public async Task<ApiResultado<object>> ReservarViajeEnCursoAsync(int idViaje, string idPasajero, int numeroAsiento)
        {
            var request = new
            {
                idPasajero = idPasajero,
                numeroAsiento = numeroAsiento
            };

            return await PostAsync<object, object>($"api/viajesencurso/{idViaje}/reservar", request);
        }

        public async Task<ApiResultado<object>> CancelarReservaViajeEnCursoAsync(int idReserva)
        {
            try
            {
                HttpResponseMessage response =
                    await _httpClient.DeleteAsync($"api/viajesencurso/reservas/{idReserva}");

                return await LeerRespuestaAsync<object>(response);
            }
            catch
            {
                return ApiError<object>("No se pudo conectar con la API.");
            }
        }

        public async Task<ApiResultado<object>> FinalizarViajeEnCursoAsync(int idViaje)
        {
            var request = new { };
            return await PutAsync<object, object>($"api/viajesencurso/{idViaje}/finalizar", request);
        }

        public async Task<ApiResultado<List<Reserva>>> ObtenerMisViajesAsync(string nombreUsuario)
        {
            return await GetAsync<List<Reserva>>(
                $"api/misviajes/{Uri.EscapeDataString(nombreUsuario)}");
        }

        public async Task<ApiResultado<Reserva>> ObtenerDetalleMisViajeAsync(string nombreUsuario, int idReserva)
        {
            return await GetAsync<Reserva>(
                $"api/misviajes/{Uri.EscapeDataString(nombreUsuario)}/{idReserva}");
        }

        private async Task<ApiResultado<TResponse>> GetAsync<TResponse>(string url)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                return await LeerRespuestaAsync<TResponse>(response);
            }
            catch
            {
                return ApiError<TResponse>("No se pudo conectar con la API.");
            }
        }

        private async Task<ApiResultado<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, request);
                return await LeerRespuestaAsync<TResponse>(response);
            }
            catch
            {
                return ApiError<TResponse>("No se pudo conectar con la API.");
            }
        }

        private async Task<ApiResultado<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest request)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PutAsJsonAsync(url, request);
                return await LeerRespuestaAsync<TResponse>(response);
            }
            catch
            {
                return ApiError<TResponse>("No se pudo conectar con la API.");
            }
        }

        private async Task<ApiResultado<T>> LeerRespuestaAsync<T>(HttpResponseMessage response)
        {
            string contenido = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
            {
                return ApiError<T>("La API no devolvió contenido.");
            }

            ApiResultado<T>? resultado = JsonSerializer.Deserialize<ApiResultado<T>>(contenido, _jsonOptions);

            if (resultado == null)
            {
                return ApiError<T>("La respuesta de la API no tiene el formato esperado.");
            }

            return resultado;
        }

        private ApiResultado<T> ApiError<T>(string mensaje)
        {
            return new ApiResultado<T>
            {
                Exito = false,
                Mensaje = mensaje,
                Datos = default
            };
        }
    }
}
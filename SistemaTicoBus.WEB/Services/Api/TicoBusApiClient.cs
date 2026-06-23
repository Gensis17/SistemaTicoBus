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
using System.Net.Http.Json;
using System.Text.Json;
using SistemaTicoBus.MAUI.Models;

namespace SistemaTicoBus.MAUI.Services
{
    public class TicoBusApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        private const string ApiKey = "TICOBUS_API_KEY_DESARROLLO";
        private const string ApiKeyHeader = "X-API-KEY";

        // Este es el API publicado en Azure
        private const string BaseUrl = "https://appticobus-api2-arhybxeygycydjax.canadacentral-01.azurewebsites.net/";
        public TicoBusApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };

            _httpClient.DefaultRequestHeaders.Add(ApiKeyHeader, ApiKey);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ApiRespuesta<LoginRespuesta>> LoginAsync(string usuario, string clave)
        {
            try
            {
                var solicitud = new { username = usuario, password = clave };
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/auth/login", solicitud);
                string contenido = await response.Content.ReadAsStringAsync();

                var resultado = JsonSerializer.Deserialize<ApiRespuesta<LoginRespuesta>>(contenido, _jsonOptions);
                return resultado ?? ErrorGenerico<LoginRespuesta>();
            }
            catch
            {
                return ErrorConexion<LoginRespuesta>();
            }
        }

        public async Task<ApiRespuesta<List<ReservaModelo>>> ObtenerMisReservasAsync(string nombreUsuario)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(
                    $"api/misviajes/{Uri.EscapeDataString(nombreUsuario)}");
                string contenido = await response.Content.ReadAsStringAsync();

                var resultado = JsonSerializer.Deserialize<ApiRespuesta<List<ReservaModelo>>>(contenido, _jsonOptions);
                return resultado ?? ErrorGenerico<List<ReservaModelo>>();
            }
            catch
            {
                return ErrorConexion<List<ReservaModelo>>();
            }
        }

        public async Task<ApiRespuesta<ReservaModelo>> ObtenerDetalleReservaAsync(string nombreUsuario, int idReserva)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(
                    $"api/misviajes/{Uri.EscapeDataString(nombreUsuario)}/{idReserva}");
                string contenido = await response.Content.ReadAsStringAsync();

                var resultado = JsonSerializer.Deserialize<ApiRespuesta<ReservaModelo>>(contenido, _jsonOptions);
                return resultado ?? ErrorGenerico<ReservaModelo>();
            }
            catch
            {
                return ErrorConexion<ReservaModelo>();
            }
        }

        private ApiRespuesta<T> ErrorConexion<T>()
        {
            return new ApiRespuesta<T>
            {
                Exito = false,
                Mensaje = "No se pudo conectar con el servidor."
            };
        }

        private ApiRespuesta<T> ErrorGenerico<T>()
        {
            return new ApiRespuesta<T>
            {
                Exito = false,
                Mensaje = "La respuesta del servidor no tiene el formato esperado."
            };
        }
    }
}
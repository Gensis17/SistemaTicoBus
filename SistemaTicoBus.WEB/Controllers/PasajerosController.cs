using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;
using System.Text.RegularExpressions;

namespace SistemaTicoBus.WEB.Controllers
{
    public class PasajerosController : Controller
    {
        private const string RolChofer = "Chofer";

        private readonly ITicoBusApiClient _apiClient;

        public PasajerosController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> ListadoPasajeros(string? buscarNombre, string? identificacionEditar)
        {
            if (!UsuarioEsChofer())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<List<Pasajero>> resultado = await _apiClient.ObtenerPasajerosAsync(buscarNombre);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = ObtenerMensajeSeguro(resultado.Mensaje, "No se pudieron cargar los pasajeros.");
                ViewBag.Busqueda = buscarNombre;
                return View(new List<Pasajero>());
            }

            ViewBag.Busqueda = buscarNombre;

            if (!string.IsNullOrWhiteSpace(identificacionEditar))
            {
                ApiResultado<Pasajero> pasajeroResultado =
                    await _apiClient.ObtenerPasajeroAsync(identificacionEditar);

                if (pasajeroResultado.Exito && pasajeroResultado.Datos != null)
                {
                    ViewBag.PasajeroEditar = pasajeroResultado.Datos;
                    ViewBag.IdOriginal = identificacionEditar;
                }
                else
                {
                    TempData["MensajeError"] = ObtenerMensajeSeguro(
                        pasajeroResultado.Mensaje,
                        "No se pudo cargar el pasajero seleccionado para editar."
                    );
                }
            }

            return View(resultado.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPasajeroGuardar(Pasajero model)
        {
            if (!UsuarioEsChofer())
            {
                return RedirectToAction("Login", "Account");
            }

            NormalizarPasajero(model);

            ApiResultado<Pasajero> resultado = await _apiClient.CrearPasajeroAsync(model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = ObtenerMensajeSeguro(
                    resultado.Mensaje,
                    "No se pudo registrar el pasajero. Verifique los datos ingresados."
                );
            }
            else
            {
                TempData["MensajeExito"] = ObtenerMensajeSeguro(
                    resultado.Mensaje,
                    "Pasajero registrado correctamente."
                );
            }

            return RedirectToAction(nameof(ListadoPasajeros));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPasajeroGuardar(Pasajero model, string idOriginal)
        {
            if (!UsuarioEsChofer())
            {
                return RedirectToAction("Login", "Account");
            }

            idOriginal = idOriginal?.Trim() ?? string.Empty;
            NormalizarPasajero(model);

            if (string.IsNullOrWhiteSpace(idOriginal))
            {
                TempData["MensajeError"] = "No se recibió la identificación original del pasajero.";
                return RedirectToAction(nameof(ListadoPasajeros));
            }

            ApiResultado<Pasajero> resultado = await _apiClient.EditarPasajeroAsync(idOriginal, model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = ObtenerMensajeSeguro(
                    resultado.Mensaje,
                    "No se pudo actualizar el pasajero. Verifique los datos ingresados."
                );

                return RedirectToAction(nameof(ListadoPasajeros), new { identificacionEditar = idOriginal });
            }

            TempData["MensajeExito"] = ObtenerMensajeSeguro(
                resultado.Mensaje,
                "Pasajero actualizado correctamente."
            );

            return RedirectToAction(nameof(ListadoPasajeros));
        }

        private bool UsuarioEsChofer()
        {
            string rol = (HttpContext.Session.GetString("Rol") ?? string.Empty).Trim();

            return string.Equals(rol, RolChofer, StringComparison.OrdinalIgnoreCase);
        }

        private void NormalizarPasajero(Pasajero model)
        {
            model.Identificacion = NormalizarTexto(model.Identificacion);
            model.Nombre = NormalizarTexto(model.Nombre);
            model.Apellidos = NormalizarTexto(model.Apellidos);
            model.Correo = NormalizarTexto(model.Correo).ToLowerInvariant();
            model.Clave = string.IsNullOrWhiteSpace(model.Clave) ? "Pasa123*" : model.Clave.Trim();
            model.Rol = "Pasajero";
        }

        private string NormalizarTexto(string? texto)
        {
            texto = texto?.Trim() ?? string.Empty;
            texto = Regex.Replace(texto, @"\s+", " ");
            return texto;
        }

        private string ObtenerMensajeSeguro(string? mensajeApi, string mensajeRespaldo)
        {
            return string.IsNullOrWhiteSpace(mensajeApi)
                ? mensajeRespaldo
                : mensajeApi.Trim();
        }
    }
}
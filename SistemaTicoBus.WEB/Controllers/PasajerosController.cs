using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class PasajerosController : Controller
    {
        private const string RolAdministrador = "Administrador";

        private readonly ITicoBusApiClient _apiClient;

        public PasajerosController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> ListadoPasajeros(string buscarNombre, string identificacionEditar)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<List<Pasajero>> resultado = await _apiClient.ObtenerPasajerosAsync(buscarNombre);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                ViewBag.Busqueda = buscarNombre;
                return View(new List<Pasajero>());
            }

            ViewBag.Busqueda = buscarNombre;

            if (!string.IsNullOrWhiteSpace(identificacionEditar))
            {
                ApiResultado<Pasajero> pasajeroResultado =
                    await _apiClient.ObtenerPasajeroAsync(identificacionEditar);

                if (pasajeroResultado.Exito)
                {
                    ViewBag.PasajeroEditar = pasajeroResultado.Datos;
                    ViewBag.IdOriginal = identificacionEditar;
                }
            }

            return View(resultado.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPasajeroGuardar(Pasajero model)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            NormalizarPasajero(model);

            ApiResultado<Pasajero> resultado = await _apiClient.CrearPasajeroAsync(model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(ListadoPasajeros));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPasajeroGuardar(Pasajero model, string idOriginal)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            NormalizarPasajero(model);

            if (string.IsNullOrWhiteSpace(idOriginal))
            {
                TempData["MensajeError"] = "No se recibió la identificación original del pasajero.";
                return RedirectToAction(nameof(ListadoPasajeros));
            }

            ApiResultado<Pasajero> resultado = await _apiClient.EditarPasajeroAsync(idOriginal, model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(ListadoPasajeros));
        }

        private bool UsuarioEsAdministrador()
        {
            string? rol = HttpContext.Session.GetString("Rol");
            return rol == RolAdministrador;
        }

        private void NormalizarPasajero(Pasajero model)
        {
            model.Identificacion = model.Identificacion?.Trim() ?? string.Empty;
            model.Nombre = model.Nombre?.Trim() ?? string.Empty;
            model.Apellidos = model.Apellidos?.Trim() ?? string.Empty;
            model.Correo = model.Correo?.Trim() ?? string.Empty;
        }
    }
}
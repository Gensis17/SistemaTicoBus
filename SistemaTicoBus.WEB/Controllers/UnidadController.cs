using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class UnidadController : Controller
    {
        private const string RolAdministrador = "Administrador";

        private readonly ITicoBusApiClient _apiClient;

        public UnidadController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<List<Unidad>> resultado = await _apiClient.ObtenerUnidadesAsync();

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return View(new List<Unidad>());
            }

            return View(resultado.Datos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Unidad model)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            NormalizarUnidad(model);

            ApiResultado<Unidad> resultado = await _apiClient.CrearUnidadAsync(model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Unidad model, string placaOriginal)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            NormalizarUnidad(model);

            ApiResultado<Unidad> resultado = await _apiClient.EditarUnidadAsync(placaOriginal, model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
            }
            else
            {
                TempData["MensajeExito"] = resultado.Mensaje;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioEsAdministrador()
        {
            string? rol = HttpContext.Session.GetString("Rol");
            return rol == RolAdministrador;
        }

        private void NormalizarUnidad(Unidad model)
        {
            model.Placa = model.Placa?.Trim() ?? string.Empty;
            model.Modelo = model.Modelo?.Trim() ?? string.Empty;
        }
    }
}

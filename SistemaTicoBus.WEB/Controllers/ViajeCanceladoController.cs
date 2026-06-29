using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajeCanceladoController : Controller
    {
        private const string RolAdministrador = "Administrador";
        private const string RolChofer = "Chofer";

        private readonly ITicoBusApiClient _apiClient;

        public ViajeCanceladoController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!UsuarioPuedeAcceder())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<List<ViajeCancelado>> resultado = await _apiClient.ObtenerViajesCanceladosAsync();

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return View(new List<ViajeCancelado>());
            }

            return View(resultado.Datos);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            if (!UsuarioPuedeAcceder())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<ViajeCancelado> resultado = await _apiClient.ObtenerDetalleViajeCanceladoAsync(id);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            return View(resultado.Datos);
        }

        private bool UsuarioPuedeAcceder()
        {
            string? rol = HttpContext.Session.GetString("Rol");
            return rol == RolAdministrador || rol == RolChofer;
        }
    }
}
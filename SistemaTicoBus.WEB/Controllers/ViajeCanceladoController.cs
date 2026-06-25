using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ViajeCanceladoController : Controller
    {
        private const string RolAdministrador = "Administrador";

        private readonly ITicoBusApiClient _apiClient;

        public ViajeCanceladoController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            if (!UsuarioEsAdministrador())
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

        public async Task<IActionResult> Detalle(int id)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            ApiResultado<ViajeCancelado> resultado = await _apiClient.ObtenerDetalleViajeCanceladoAsync(id);

            if (!resultado.Exito || resultado.Datos == null)
            {
                return NotFound();
            }

            return View(resultado.Datos);
        }

        private bool UsuarioEsAdministrador()
        {
            string? rol = HttpContext.Session.GetString("Rol");
            return rol == RolAdministrador;
        }
    }
}

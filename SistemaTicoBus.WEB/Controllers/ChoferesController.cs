using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.WEB.Models;
using SistemaTicoBus.WEB.Services.Api;

namespace SistemaTicoBus.WEB.Controllers
{
    public class ChoferesController : Controller
    {
        private const string RolAdministrador = "Administrador";

        private readonly ITicoBusApiClient _apiClient;

        public ChoferesController(ITicoBusApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            // Listar choferes ahora consume la API con API Key.
            ApiResultado<List<ChoferViewModel>> resultado = await _apiClient.ObtenerChoferesAsync(busqueda);

            if (!resultado.Exito || resultado.Datos == null)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                ViewBag.Busqueda = busqueda;
                return View(new List<ChoferViewModel>());
            }

            ViewBag.Busqueda = busqueda;
            return View(resultado.Datos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChoferViewModel model)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            NormalizarChofer(model);

            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Verifique los datos del chofer. Todos los campos son requeridos y el correo debe tener formato válido.";
                return RedirectToAction(nameof(Index));
            }

            // Agregar chofer ahora consume la API con API Key.
            ApiResultado<ChoferViewModel> resultado = await _apiClient.CrearChoferAsync(model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            TempData["MensajeExito"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ChoferViewModel model)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["MensajeError"] = "No se recibió la identificación actual del chofer.";
                return RedirectToAction(nameof(Index));
            }

            NormalizarChofer(model);

            // En edición no se cambia correo ni clave generada.
            ModelState.Remove(nameof(ChoferViewModel.Correo));
            ModelState.Remove(nameof(ChoferViewModel.NombreUsuario));
            ModelState.Remove(nameof(ChoferViewModel.ClaveGenerada));

            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Verifique los datos del chofer. Identificación, nombre y apellidos son requeridos.";
                return RedirectToAction(nameof(Index));
            }

            // Editar chofer ahora consume la API con API Key.
            ApiResultado<ChoferViewModel> resultado = await _apiClient.EditarChoferAsync(id, model);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            TempData["MensajeExito"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!UsuarioEsAdministrador())
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["MensajeError"] = "No se recibió la identificación del chofer a eliminar.";
                return RedirectToAction(nameof(Index));
            }

            // Eliminar no está en el punto de la segunda entrega para módulo 2,
            // pero se deja pasando por API porque la vista ya lo tiene.
            ApiResultado<object> resultado = await _apiClient.EliminarChoferAsync(id);

            if (!resultado.Exito)
            {
                TempData["MensajeError"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            TempData["MensajeExito"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioEsAdministrador()
        {
            string? rol = HttpContext.Session.GetString("Rol");
            return rol == RolAdministrador;
        }

        private void NormalizarChofer(ChoferViewModel model)
        {
            model.Identificacion = model.Identificacion?.Trim() ?? string.Empty;
            model.Nombre = model.Nombre?.Trim() ?? string.Empty;
            model.Apellidos = model.Apellidos?.Trim() ?? string.Empty;
            model.Correo = model.Correo?.Trim() ?? string.Empty;
        }
    }
}
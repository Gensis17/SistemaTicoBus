using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.WEB.Models;
using SistemaTicoBus.WEB.Services.Api;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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

            string? mensajeValidacion = ValidarChoferParaCrear(model);

            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
            {
                TempData["MensajeError"] = mensajeValidacion;
                return RedirectToAction(nameof(Index));
            }

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

            id = id?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["MensajeError"] = "No se recibió la cédula actual del chofer.";
                return RedirectToAction(nameof(Index));
            }

            NormalizarChofer(model);

            string? mensajeValidacion = ValidarChoferParaEditar(model);

            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
            {
                TempData["MensajeError"] = mensajeValidacion;
                return RedirectToAction(nameof(Index));
            }

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

            id = id?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["MensajeError"] = "No se recibió la cédula del chofer a eliminar.";
                return RedirectToAction(nameof(Index));
            }

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
            string rol = (HttpContext.Session.GetString("Rol") ?? string.Empty).Trim();

            return string.Equals(rol, RolAdministrador, StringComparison.OrdinalIgnoreCase);
        }

        private void NormalizarChofer(ChoferViewModel model)
        {
            model.Identificacion = NormalizarTexto(model.Identificacion);
            model.Nombre = NormalizarTexto(model.Nombre);
            model.Apellidos = NormalizarTexto(model.Apellidos);
            model.Correo = NormalizarTexto(model.Correo).ToLowerInvariant();
        }

        private string NormalizarTexto(string? texto)
        {
            texto = texto?.Trim() ?? string.Empty;
            texto = Regex.Replace(texto, @"\s+", " ");
            return texto;
        }

        private string? ValidarChoferParaCrear(ChoferViewModel model)
        {
            string? mensajeBase = ValidarDatosBasicosChofer(model, validarCorreo: true);

            if (!string.IsNullOrWhiteSpace(mensajeBase))
            {
                return mensajeBase;
            }

            return null;
        }

        private string? ValidarChoferParaEditar(ChoferViewModel model)
        {
            string? mensajeBase = ValidarDatosBasicosChofer(model, validarCorreo: false);

            if (!string.IsNullOrWhiteSpace(mensajeBase))
            {
                return mensajeBase;
            }

            return null;
        }

        private string? ValidarDatosBasicosChofer(ChoferViewModel model, bool validarCorreo)
        {
            if (string.IsNullOrWhiteSpace(model.Identificacion))
            {
                return "La cédula es requerida.";
            }

            if (!Regex.IsMatch(model.Identificacion, @"^\d+$"))
            {
                return "La cédula solo puede contener números. No use letras, espacios ni guiones.";
            }

            if (model.Identificacion.Length < 6 || model.Identificacion.Length > 20)
            {
                return "La cédula debe tener entre 6 y 20 números.";
            }

            if (string.IsNullOrWhiteSpace(model.Nombre))
            {
                return "El nombre es requerido.";
            }

            if (!Regex.IsMatch(model.Nombre, @"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü]+(?: [A-Za-zÁÉÍÓÚáéíóúÑñÜü]+)*$"))
            {
                return "El nombre solo puede contener letras y espacios.";
            }

            if (model.Nombre.Length < 2 || model.Nombre.Length > 50)
            {
                return "El nombre debe tener entre 2 y 50 caracteres.";
            }

            if (string.IsNullOrWhiteSpace(model.Apellidos))
            {
                return "Los apellidos son requeridos.";
            }

            if (!Regex.IsMatch(model.Apellidos, @"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü]+(?: [A-Za-zÁÉÍÓÚáéíóúÑñÜü]+)*$"))
            {
                return "Los apellidos solo pueden contener letras y espacios.";
            }

            if (model.Apellidos.Length < 2 || model.Apellidos.Length > 50)
            {
                return "Los apellidos deben tener entre 2 y 50 caracteres.";
            }

            if (validarCorreo)
            {
                if (string.IsNullOrWhiteSpace(model.Correo))
                {
                    return "El correo electrónico es requerido.";
                }

                if (model.Correo.Length > 100)
                {
                    return "El correo electrónico no puede superar los 100 caracteres.";
                }

                EmailAddressAttribute validadorCorreo = new EmailAddressAttribute();

                if (!validadorCorreo.IsValid(model.Correo))
                {
                    return "Ingrese un correo electrónico válido.";
                }
            }

            return null;
        }
    }
}
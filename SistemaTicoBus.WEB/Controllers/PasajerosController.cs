using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.BL;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.WEB.Controllers
{
    public class PasajerosController : Controller
    {
        private readonly PasajeroRepositorio _repository = new PasajeroRepositorio();
        private readonly ReservaBL _reservaBL = new ReservaBL();

        public IActionResult ListadoPasajeros(string buscarNombre, string identificacionEditar)
        {
            var pasajeros = _repository.ObtenerPasajeros(buscarNombre);
            ViewBag.Busqueda = buscarNombre;

            if (!string.IsNullOrEmpty(identificacionEditar))
            {
                var pasajero = _repository.ObtenerPasajeroPorId(identificacionEditar);
                ViewBag.PasajeroEditar = pasajero;
                ViewBag.IdOriginal = identificacionEditar;
            }

            return View(pasajeros);
        }

        [HttpPost]
        public IActionResult RegistrarPasajeroGuardar(Pasajero model)
        {
            if (!string.IsNullOrEmpty(model.Identificacion) &&
                !string.IsNullOrEmpty(model.Nombre) &&
                !string.IsNullOrEmpty(model.Apellidos) &&
                !string.IsNullOrEmpty(model.Correo))
            {
                try
                {
                    model.Clave = "Pasa123*";
                    model.Rol = "Pasajero";

                    _repository.RegistrarPasajero(model);

                    TempData["MensajeExito"] = $"Pasajero registrado con éxito. Cuenta creada para: {model.Correo}";
                }
                catch (InvalidOperationException ex)
                {
                    TempData["MensajeError"] = ex.Message;
                }
                catch (Exception)
                {
                    TempData["MensajeError"] = "Ocurrió un error inesperado al registrar el pasajero. Intente de nuevo.";
                }
            }
            else
            {
                TempData["MensajeError"] = "Todos los campos son requeridos: Identificación, Nombre, Apellidos y Correo.";
            }

            return RedirectToAction("ListadoPasajeros");
        }

        [HttpPost]
        public IActionResult EditarPasajeroGuardar(Pasajero model, string idOriginal)
        {
            if (!string.IsNullOrEmpty(model.Identificacion) &&
                !string.IsNullOrEmpty(model.Nombre) &&
                !string.IsNullOrEmpty(model.Apellidos) &&
                !string.IsNullOrEmpty(model.Correo) &&
                !string.IsNullOrEmpty(idOriginal))
            {
                try
                {
                    _repository.EditarPasajero(model, idOriginal);
                    TempData["MensajeExito"] = "Los datos del pasajero fueron actualizados con éxito.";
                }
                catch (InvalidOperationException ex)
                {
                    TempData["MensajeError"] = ex.Message;
                }
                catch (Exception)
                {
                    TempData["MensajeError"] = "Ocurrió un error inesperado al actualizar el pasajero. Intente de nuevo.";
                }
            }
            else
            {
                TempData["MensajeError"] = "No se pudieron guardar los cambios. Verifique que no queden campos vacíos.";
            }

            return RedirectToAction("ListadoPasajeros");
        }
    }
}

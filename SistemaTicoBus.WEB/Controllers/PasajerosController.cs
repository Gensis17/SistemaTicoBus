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
            // Filtrado exclusivo por Nombre directo en la BD
            var pasajeros = _repository.ObtenerPasajeros(buscarNombre);
            ViewBag.Busqueda = buscarNombre;

            // Si se presionó Editar, cargamos el pasajero para el formulario
            if (!string.IsNullOrEmpty(identificacionEditar))
            {
                var pasajero = _repository.ObtenerPasajeroPorId(identificacionEditar);
                ViewBag.PasajeroEditar = pasajero;
                ViewBag.IdOriginal = identificacionEditar; // Guardamos la cédula original antes de ser editada
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
                // Mantenemos la clave por defecto que usa tu base de datos para evitar conflictos de logueo
                model.Clave = "Pasa123*";
                model.Rol = "Pasajero";

                // Se envía el modelo completo con el correo ingresado por el usuario
                _repository.RegistrarPasajero(model);

                TempData["CorreoSimulado"] = $"Pasajero registrado con éxito. Cuenta de acceso creada para el correo: {model.Correo}";
            }
            else
            {
                // Alerta por si falta rellenar algún espacio obligatorio
                TempData["CorreoSimulado"] = "⚠️ Error: Todos los campos son requeridos (Identificación, Nombre, Apellidos y Correo).";
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
                // Pasamos el modelo con el correo actualizado y la cédula original para aplicar los cambios en cascada
                _repository.EditarPasajero(model, idOriginal);

                TempData["CorreoSimulado"] = $"Los datos y correo del pasajero fueron actualizados con éxito.";
            }
            else
            {
                TempData["CorreoSimulado"] = "⚠️ Error: No se pudieron guardar los cambios. Verifique que no queden datos vacíos.";
            }

            return RedirectToAction("ListadoPasajeros");
        }


    }
}

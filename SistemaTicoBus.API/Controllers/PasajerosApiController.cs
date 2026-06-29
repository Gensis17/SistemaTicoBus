using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/pasajeros")]
    public class PasajerosApiController : ControllerBase
    {
        private readonly PasajeroRepositorio _repository;

        public PasajerosApiController()
        {
            _repository = new PasajeroRepositorio();
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<Pasajero>>> Listar([FromQuery] string? buscarNombre)
        {
            try
            {
                List<Pasajero> pasajeros = _repository.ObtenerPasajeros(buscarNombre);
                return Ok(ApiRespuesta<List<Pasajero>>.Ok(pasajeros));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiRespuesta<List<Pasajero>>.Error("No se pudieron cargar los pasajeros.")
                );
            }
        }

        [HttpGet("{identificacion}")]
        public ActionResult<ApiRespuesta<Pasajero>> ObtenerPorId(string identificacion)
        {
            try
            {
                identificacion = identificacion?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(identificacion))
                {
                    return BadRequest(ApiRespuesta<Pasajero>.Error("La identificación del pasajero es requerida."));
                }

                Pasajero? pasajero = _repository.ObtenerPasajeroPorId(identificacion);

                if (pasajero == null)
                {
                    return NotFound(ApiRespuesta<Pasajero>.Error("No se encontró el pasajero."));
                }

                return Ok(ApiRespuesta<Pasajero>.Ok(pasajero));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiRespuesta<Pasajero>.Error("No se pudo obtener el pasajero.")
                );
            }
        }

        [HttpPost]
        public ActionResult<ApiRespuesta<Pasajero>> Agregar(Pasajero model)
        {
            NormalizarPasajero(model);

            string? mensajeValidacion = ValidarPasajero(model);

            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error(mensajeValidacion));
            }

            try
            {
                model.Clave = "Pasa123*";
                model.Rol = "Pasajero";

                _repository.RegistrarPasajero(model);

                return Ok(ApiRespuesta<Pasajero>.Ok(
                    model,
                    "Pasajero registrado correctamente."
                ));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error(ex.Message));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiRespuesta<Pasajero>.Error("Ocurrió un error al registrar el pasajero.")
                );
            }
        }

        [HttpPut("{idOriginal}")]
        public ActionResult<ApiRespuesta<Pasajero>> Editar(string idOriginal, Pasajero model)
        {
            idOriginal = idOriginal?.Trim() ?? string.Empty;
            NormalizarPasajero(model);

            if (string.IsNullOrWhiteSpace(idOriginal))
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error("No se recibió la identificación original del pasajero."));
            }

            string? mensajeValidacion = ValidarPasajero(model);

            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error(mensajeValidacion));
            }

            try
            {
                _repository.EditarPasajero(model, idOriginal);

                return Ok(ApiRespuesta<Pasajero>.Ok(
                    model,
                    "Pasajero actualizado correctamente."
                ));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error(ex.Message));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiRespuesta<Pasajero>.Error("Ocurrió un error al actualizar el pasajero.")
                );
            }
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

        private string? ValidarPasajero(Pasajero model)
        {
            if (string.IsNullOrWhiteSpace(model.Identificacion))
            {
                return "La cédula es requerida.";
            }

            if (!Regex.IsMatch(model.Identificacion, @"^[0-9\-]+$"))
            {
                return "La cédula solo puede contener números y guiones. No use letras ni otros símbolos.";
            }

            string cedulaSinGuiones = model.Identificacion.Replace("-", "");

            if (cedulaSinGuiones.Length < 6 || cedulaSinGuiones.Length > 20)
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

            return null;
        }
    }
}
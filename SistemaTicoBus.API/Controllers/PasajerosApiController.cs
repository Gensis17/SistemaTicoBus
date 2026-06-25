using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;

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
                var pasajeros = _repository.ObtenerPasajeros(buscarNombre);
                return Ok(ApiRespuesta<List<Pasajero>>.Ok(pasajeros));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<List<Pasajero>>.Error("No se pudieron cargar los pasajeros."));
            }
        }

        [HttpPost]
        public ActionResult<ApiRespuesta<Pasajero>> Agregar(Pasajero model)
        {
            if (string.IsNullOrWhiteSpace(model.Identificacion) ||
                string.IsNullOrWhiteSpace(model.Nombre) ||
                string.IsNullOrWhiteSpace(model.Apellidos) ||
                string.IsNullOrWhiteSpace(model.Correo))
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error("Todos los campos son requeridos."));
            }

            try
            {
                model.Clave = "Pasa123*";
                model.Rol = "Pasajero";

                _repository.RegistrarPasajero(model);

                return Ok(ApiRespuesta<Pasajero>.Ok(model, "Pasajero registrado correctamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error(ex.Message));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Pasajero>.Error("Ocurrió un error al registrar el pasajero."));
            }
        }

        [HttpPut("{idOriginal}")]
        public ActionResult<ApiRespuesta<Pasajero>> Editar(string idOriginal, Pasajero model)
        {
            if (string.IsNullOrWhiteSpace(idOriginal) ||
                string.IsNullOrWhiteSpace(model.Identificacion) ||
                string.IsNullOrWhiteSpace(model.Nombre) ||
                string.IsNullOrWhiteSpace(model.Apellidos) ||
                string.IsNullOrWhiteSpace(model.Correo))
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error("No se pudieron guardar los cambios. Verifique los campos."));
            }

            try
            {
                _repository.EditarPasajero(model, idOriginal);
                return Ok(ApiRespuesta<Pasajero>.Ok(model, "Pasajero actualizado correctamente."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error(ex.Message));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Pasajero>.Error("Ocurrió un error al actualizar el pasajero."));
            }
        }
        [HttpGet("{identificacion}")]
        public ActionResult<ApiRespuesta<Pasajero>> ObtenerPorId(string identificacion)
        {
            try
            {
                var pasajero = _repository.ObtenerPasajeroPorId(identificacion);

                if (pasajero == null)
                {
                    return NotFound(ApiRespuesta<Pasajero>.Error("No se encontró el pasajero."));
                }

                return Ok(ApiRespuesta<Pasajero>.Ok(pasajero));
            }
            catch
            {
                return StatusCode(500,
                    ApiRespuesta<Pasajero>.Error("No se pudo obtener el pasajero."));
            }
        }
    }
}
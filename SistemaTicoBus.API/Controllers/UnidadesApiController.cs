using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/unidades")]
    public class UnidadesApiController : ControllerBase
    {
        private readonly UnidadBL _unidadBL;

        public UnidadesApiController(UnidadBL unidadBL)
        {
            _unidadBL = unidadBL;
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<Unidad>>> Listar()
        {
            try
            {
                var lista = _unidadBL.Listar();
                return Ok(ApiRespuesta<List<Unidad>>.Ok(lista));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<List<Unidad>>.Error("No se pudieron cargar las unidades."));
            }
        }

        [HttpPost]
        public ActionResult<ApiRespuesta<Unidad>> Agregar(Unidad unidad)
        {
            try
            {
                string mensaje = _unidadBL.Agregar(unidad);

                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    return BadRequest(ApiRespuesta<Unidad>.Error(mensaje));
                }

                return Ok(ApiRespuesta<Unidad>.Ok(unidad, "Unidad agregada correctamente."));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Unidad>.Error("Ocurrió un error al agregar la unidad."));
            }
        }

        [HttpPut("{placaOriginal}")]
        public ActionResult<ApiRespuesta<Unidad>> Editar(string placaOriginal, Unidad unidad)
        {
            try
            {
                string mensaje = _unidadBL.Editar(unidad, placaOriginal);

                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    return BadRequest(ApiRespuesta<Unidad>.Error(mensaje));
                }

                return Ok(ApiRespuesta<Unidad>.Ok(unidad, "Unidad editada correctamente."));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Unidad>.Error("Ocurrió un error al editar la unidad."));
            }
        }
    }
}
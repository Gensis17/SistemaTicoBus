using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/viajescancelados")]
    public class ViajesCanceladosApiController : ControllerBase
    {
        private readonly ViajeCanceladoBL _bl;

        public ViajesCanceladosApiController(ViajeCanceladoBL bl)
        {
            _bl = bl;
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<ViajeCancelado>>> Listar()
        {
            try
            {
                var lista = _bl.ListarViajesCancelados();
                return Ok(ApiRespuesta<List<ViajeCancelado>>.Ok(lista));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<List<ViajeCancelado>>.Error("No se pudieron cargar los viajes cancelados."));
            }
        }

        [HttpGet("{id}")]
        public ActionResult<ApiRespuesta<ViajeCancelado>> Detalle(int id)
        {
            try
            {
                var viaje = _bl.ObtenerDetalleViajeCancelado(id);

                if (viaje == null)
                {
                    return NotFound(ApiRespuesta<ViajeCancelado>.Error("No se encontró el viaje cancelado."));
                }

                return Ok(ApiRespuesta<ViajeCancelado>.Ok(viaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<ViajeCancelado>.Error("No se pudo cargar el detalle del viaje cancelado."));
            }
        }
    }
}
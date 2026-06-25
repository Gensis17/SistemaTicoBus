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
        private readonly string _connectionString;

        public ViajesCanceladosApiController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<ViajeCancelado>>> Listar()
        {
            try
            {
                ViajeCanceladoBL bl = new ViajeCanceladoBL(_connectionString);
                var lista = bl.ListarViajesCancelados();

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
                ViajeCanceladoBL bl = new ViajeCanceladoBL(_connectionString);
                var viaje = bl.ObtenerDetalleViajeCancelado(id);

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
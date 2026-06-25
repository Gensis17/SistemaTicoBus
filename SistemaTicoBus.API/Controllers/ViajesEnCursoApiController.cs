using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/viajesencurso")]
    public class ViajesEnCursoApiController : ControllerBase
    {
        private readonly ViajesEnCursoBL _viajesBL;

        public ViajesEnCursoApiController(ViajesEnCursoBL viajesBL)
        {
            _viajesBL = viajesBL;
        }

        [HttpGet]
        public async Task<ActionResult<ApiRespuesta<List<Viaje>>>> Listar()
        {
            try
            {
                var viajes = await _viajesBL.ObtenerViajesActivosAsync();
                return Ok(ApiRespuesta<List<Viaje>>.Ok(viajes));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<List<Viaje>>.Error("No se pudieron cargar los viajes en curso."));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiRespuesta<Viaje>>> Detalle(int id)
        {
            try
            {
                var viaje = await _viajesBL.ObtenerDetalleViajeAsync(id);

                if (viaje == null)
                {
                    return NotFound(ApiRespuesta<Viaje>.Error("No se encontró el viaje."));
                }

                return Ok(ApiRespuesta<Viaje>.Ok(viaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Viaje>.Error("No se pudo cargar el detalle del viaje."));
            }
        }

        [HttpGet("pasajeros")]
        public async Task<ActionResult<ApiRespuesta<List<PasajeroCatalogoDTO>>>> CatalogoPasajeros()
        {
            try
            {
                var pasajeros = await _viajesBL.ObtenerCatalogoPasajerosAsync();
                return Ok(ApiRespuesta<List<PasajeroCatalogoDTO>>.Ok(pasajeros));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<List<PasajeroCatalogoDTO>>.Error("No se pudo cargar el catálogo de pasajeros."));
            }
        }

        [HttpPost("{id}/reservar")]
        public async Task<ActionResult<ApiRespuesta<object>>> Reservar(int id, ReservaViajeSolicitud solicitud)
        {
            try
            {
                var resultado = await _viajesBL.RegistrarReservaAsync(
                    id,
                    solicitud.IdPasajero,
                    solicitud.NumeroAsiento
                );

                if (!resultado.ComponenteExitoso)
                {
                    return BadRequest(ApiRespuesta<object>.Error(resultado.Mensaje));
                }

                return Ok(ApiRespuesta<object>.Ok(new { }, resultado.Mensaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<object>.Error("Ocurrió un error al procesar la reserva."));
            }
        }

        [HttpDelete("reservas/{idReserva}")]
        public async Task<ActionResult<ApiRespuesta<object>>> CancelarReserva(int idReserva)
        {
            try
            {
                bool cancelado = await _viajesBL.CancelarReservaAsync(idReserva);

                if (!cancelado)
                {
                    return BadRequest(ApiRespuesta<object>.Error("No se pudo cancelar la reserva."));
                }

                return Ok(ApiRespuesta<object>.Ok(new { }, "La reserva fue cancelada con éxito."));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<object>.Error("Ocurrió un error al cancelar la reserva."));
            }
        }

        [HttpPut("{id}/finalizar")]
        public async Task<ActionResult<ApiRespuesta<object>>> FinalizarViaje(int id)
        {
            try
            {
                bool finalizado = await _viajesBL.FinalizarViajeAsync(id);

                if (!finalizado)
                {
                    return BadRequest(ApiRespuesta<object>.Error("No se logró finalizar el viaje."));
                }

                return Ok(ApiRespuesta<object>.Ok(
                    new { },
                    $"El viaje #{id} ha cambiado a estado Completado."
                ));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<object>.Error("Ocurrió un error al finalizar el viaje."));
            }
        }
    }

    public class ReservaViajeSolicitud
    {
        public string IdPasajero { get; set; } = string.Empty;
        public int NumeroAsiento { get; set; }
    }
}

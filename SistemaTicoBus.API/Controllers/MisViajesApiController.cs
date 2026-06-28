using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/misviajes")]
    public class MisViajesApiController : ControllerBase
    {
        private readonly ReservaBL _reservaBL;

        public MisViajesApiController()
        {
            _reservaBL = new ReservaBL();
        }

        [HttpGet("{nombreUsuario}")]
        public ActionResult<ApiRespuesta<List<Reserva>>> Listar(string nombreUsuario)
        {
            try
            {
                var lista = _reservaBL.ObtenerMisViajes(nombreUsuario);
                return Ok(ApiRespuesta<List<Reserva>>.Ok(lista));
            }
            catch
            {
                return StatusCode(500,
                    ApiRespuesta<List<Reserva>>.Error("No se pudieron cargar los viajes del pasajero."));
            }
        }

        [HttpGet("{nombreUsuario}/{idReserva}")]
        public ActionResult<ApiRespuesta<Reserva>> Detalle(string nombreUsuario, int idReserva)
        {
            try
            {
                var reserva = _reservaBL.ObtenerDetalleMisViajes(idReserva, nombreUsuario);

                if (reserva == null)
                {
                    return NotFound(ApiRespuesta<Reserva>.Error("No se encontró la reserva."));
                }

                return Ok(ApiRespuesta<Reserva>.Ok(reserva));
            }
            catch
            {
                return StatusCode(500,
                    ApiRespuesta<Reserva>.Error("No se pudo cargar el detalle de la reserva."));
            }
        }
    }
}
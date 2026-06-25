using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL;
using SistemaTicoBus.MODEL.Entidades;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/viajes")]
    public class ViajesApiController : Controller
    {
        private readonly ViajeBL _viajeBL;

        public ViajesApiController(ViajeBL viajeBL)
        {
            _viajeBL = viajeBL;
        }

        [HttpGet]
        public ActionResult<ApiRespuesta<List<Viaje>>> Listar([FromQuery] string? filtro)
        {
            try
            {
                var viajes = _viajeBL.ObtenerViajes();

                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    viajes = viajes.Where(v =>
                        (v.Ruta != null && v.Ruta.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase)) ||
                        v.FechaHoraSalida.ToString("dd/MM/yyyy").Contains(filtro)
                    ).ToList();
                }

                return Ok(ApiRespuesta<List<Viaje>>.Ok(viajes));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<List<Viaje>>.Error("No se pudieron cargar los viajes."));
            }
        }

        [HttpGet("{id}")]
        public ActionResult<ApiRespuesta<Viaje>> ObtenerPorId(int id)
        {
            try
            {
                var viaje = _viajeBL.ObtenerViajePorId(id);

                if (viaje == null)
                {
                    return NotFound(ApiRespuesta<Viaje>.Error("No se encontró el viaje."));
                }

                return Ok(ApiRespuesta<Viaje>.Ok(viaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Viaje>.Error("No se pudo obtener el viaje."));
            }
        }

        [HttpPost]
        public ActionResult<ApiRespuesta<Viaje>> Agregar(Viaje viaje)
        {
            try
            {
                var resultado = _viajeBL.AgregarViaje(viaje);

                if (!resultado.Exitoso)
                {
                    return BadRequest(ApiRespuesta<Viaje>.Error(resultado.Mensaje));
                }

                return Ok(ApiRespuesta<Viaje>.Ok(viaje, resultado.Mensaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Viaje>.Error("Ocurrió un error al registrar el viaje."));
            }
        }

        [HttpPut("{id}")]
        public ActionResult<ApiRespuesta<Viaje>> Editar(int id, Viaje viaje)
        {
            try
            {
                viaje.IdViaje = id;

                var resultado = _viajeBL.EditarViaje(viaje);

                if (!resultado.Exitoso)
                {
                    return BadRequest(ApiRespuesta<Viaje>.Error(resultado.Mensaje));
                }

                return Ok(ApiRespuesta<Viaje>.Ok(viaje, resultado.Mensaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<Viaje>.Error("Ocurrió un error al actualizar el viaje."));
            }
        }

        [HttpPut("{id}/cancelar")]
        public ActionResult<ApiRespuesta<object>> Cancelar(int id, [FromBody] CancelarViajeSolicitud solicitud)
        {
            try
            {
                string motivo = solicitud.Motivo?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(motivo))
                {
                    return BadRequest(ApiRespuesta<object>.Error("Debe ingresar el motivo de cancelación."));
                }

                var resultado = _viajeBL.CancelarViaje(id, motivo);

                if (!resultado.Exitoso)
                {
                    return BadRequest(ApiRespuesta<object>.Error(resultado.Mensaje));
                }

                return Ok(ApiRespuesta<object>.Ok(new { }, resultado.Mensaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<object>.Error("Ocurrió un error al cancelar el viaje."));
            }
        }

        [HttpPut("{id}/iniciar")]
        public ActionResult<ApiRespuesta<object>> Iniciar(int id)
        {
            try
            {
                var resultado = _viajeBL.IniciarViaje(id);

                if (!resultado.Exitoso)
                {
                    return BadRequest(ApiRespuesta<object>.Error(resultado.Mensaje));
                }

                return Ok(ApiRespuesta<object>.Ok(new { }, resultado.Mensaje));
            }
            catch
            {
                return StatusCode(500, ApiRespuesta<object>.Error("Ocurrió un error al iniciar el viaje."));
            }
        }
    }

    public class CancelarViajeSolicitud
    {
        public string Motivo { get; set; } = string.Empty;
    }
}
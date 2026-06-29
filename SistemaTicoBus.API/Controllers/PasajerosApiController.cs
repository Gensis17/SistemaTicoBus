using Microsoft.AspNetCore.Mvc;
using SistemaTicoBus.API.Models;
using SistemaTicoBus.BL.Servicios;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaTicoBus.API.Controllers
{
    [ApiController]
    [Route("api/pasajeros")]
    public class PasajerosApiController : ControllerBase
    {
        private readonly PasajeroRepositorio _repository;
        private readonly IEmailServicio _emailServicio;

        public PasajerosApiController(PasajeroRepositorio repository, IEmailServicio emailServicio)
        {
            _repository = repository;
            _emailServicio = emailServicio;
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
        public async Task<ActionResult<ApiRespuesta<Pasajero>>> Agregar(Pasajero model)
        {
            NormalizarPasajero(model);

            string? mensajeValidacion = ValidarPasajero(model);

            if (!string.IsNullOrWhiteSpace(mensajeValidacion))
            {
                return BadRequest(ApiRespuesta<Pasajero>.Error(mensajeValidacion));
            }

            try
            {
                string claveGenerada = GenerarClaveAleatoria();

                model.Clave = claveGenerada;
                model.Rol = "Pasajero";

                string nombreUsuario = _repository.RegistrarPasajero(model);

                bool correoEnviado = await IntentarEnviarClavePasajeroAsync(
                    model.Correo,
                    nombreUsuario,
                    claveGenerada
                );

                Pasajero respuesta = new Pasajero
                {
                    Identificacion = model.Identificacion,
                    Nombre = model.Nombre,
                    Apellidos = model.Apellidos,
                    Correo = model.Correo,
                    Clave = string.Empty,
                    Rol = "Pasajero"
                };

                string mensaje = correoEnviado
                    ? "Pasajero registrado correctamente. La clave temporal fue enviada al correo indicado."
                    : "Pasajero registrado correctamente, pero no se pudo enviar el correo con la clave temporal. Revise la configuración de Mailtrap.";

                return Ok(ApiRespuesta<Pasajero>.Ok(respuesta, mensaje));
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

                Pasajero respuesta = new Pasajero
                {
                    Identificacion = model.Identificacion,
                    Nombre = model.Nombre,
                    Apellidos = model.Apellidos,
                    Correo = model.Correo,
                    Clave = string.Empty,
                    Rol = "Pasajero"
                };

                return Ok(ApiRespuesta<Pasajero>.Ok(
                    respuesta,
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

        private async Task<bool> IntentarEnviarClavePasajeroAsync(string correo, string nombreUsuario, string claveGenerada)
        {
            string asunto = "Usuario Pasajero creado — TicoBus";

            string cuerpo =
                $"Se creó su usuario de Pasajero en TicoBus.\n\n" +
                $"Nombre de usuario: {nombreUsuario}\n" +
                $"Clave temporal: {claveGenerada}\n\n" +
                $"Por seguridad, cambie su clave al ingresar al sistema.";

            try
            {
                await _emailServicio.EnviarCorreoAsync(correo, asunto, cuerpo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerarClaveAleatoria()
        {
            const string letras = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz";
            const string numeros = "23456789";
            const string especiales = "*@#";
            const string todos = letras + numeros + especiales;

            StringBuilder clave = new StringBuilder();

            clave.Append(letras[RandomNumberGenerator.GetInt32(letras.Length)]);
            clave.Append(numeros[RandomNumberGenerator.GetInt32(numeros.Length)]);
            clave.Append(especiales[RandomNumberGenerator.GetInt32(especiales.Length)]);

            for (int i = 0; i < 7; i++)
            {
                clave.Append(todos[RandomNumberGenerator.GetInt32(todos.Length)]);
            }

            return new string(
                clave
                    .ToString()
                    .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                    .ToArray()
            );
        }

        private void NormalizarPasajero(Pasajero model)
        {
            model.Identificacion = NormalizarTexto(model.Identificacion);
            model.Nombre = NormalizarTexto(model.Nombre);
            model.Apellidos = NormalizarTexto(model.Apellidos);
            model.Correo = NormalizarTexto(model.Correo).ToLowerInvariant();
            model.Clave = model.Clave?.Trim() ?? string.Empty;
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
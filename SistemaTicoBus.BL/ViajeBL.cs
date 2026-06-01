using SistemaTicoBus.BL.Servicios;
using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.BL
{
    public class ViajeBL
    {
        private readonly ViajeRepositorio _repositorio;
        private readonly IEmailServicio _emailServicio;

        public ViajeBL(ViajeRepositorio repositorio, IEmailServicio emailServicio)
        {
            _repositorio = repositorio;
            _emailServicio = emailServicio;
        }

        //Metodo para listar los viajes
        public List<Viaje> ObtenerViajes()
        {
            return _repositorio.ObtenerViajes();
        }

        // Metodo para obtener un viaje por su ID
        public Viaje? ObtenerViajePorId(int id)
        {
            return _repositorio.ObtenerViajePorId(id);
        }

        // Metodo para agregar un viaje
        public (bool Exitoso, string Mensaje) AgregarViaje(Viaje viaje)
        {
            if (_repositorio.ExisteConflictoDeDisponibilidad(
                viaje.PlacaUnidad, viaje.ChoferId,
                viaje.FechaHoraSalida, viaje.FechaHoraLlegadaEstimada))
            {
                return (false, "La unidad o el chofer seleccionado ya tiene un viaje activo en ese rango de fechas.");
            }

            _repositorio.AgregarViaje(viaje);
            return (true, "El viaje fue registrado correctamente.");
        }

        // Metodo para editar un viaje
        public (bool Exitoso, string Mensaje) EditarViaje(Viaje viaje)
        {
            if (_repositorio.ExisteConflictoDeDisponibilidad(
                viaje.PlacaUnidad, viaje.ChoferId,
                viaje.FechaHoraSalida, viaje.FechaHoraLlegadaEstimada,
                viaje.IdViaje))
            {
                return (false, "La unidad o el chofer seleccionado ya tiene un viaje activo en ese rango de fechas.");
            }

            _repositorio.EditarViaje(viaje);
            return (true, "El viaje fue actualizado correctamente.");
        }

        // Metodo para cancelar un viaje y notificar a los pasajeros con reserva
        public (bool Exitoso, string Mensaje) CancelarViaje(int idViaje, string motivo)
        {
            var correos = _repositorio.ObtenerCorreosPasajerosDelViaje(idViaje);

            _repositorio.CancelarViaje(idViaje, motivo);

            foreach (var correo in correos)
            {
                _emailServicio.EnviarCorreoAsync(
                    correo,
                    "Viaje cancelado",
                    $"El viaje #{idViaje} en el que tenía una reserva activa fue cancelado. Motivo: {motivo}."
                );
            }

            return (true, "El viaje fue cancelado y se notificó a los pasajeros con reserva.");
        }

        // INICIAR VIAJE
        public (bool Exitoso, string Mensaje) IniciarViaje(int idViaje)
        {
            _repositorio.IniciarViaje(idViaje);
            return (true, "El viaje cambió a estado En Curso.");
        }

    }
}

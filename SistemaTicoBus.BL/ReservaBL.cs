using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.BL
{
    public class ReservaBL
    {
        private readonly ReservaRepositorio _repositorio;

        public ReservaBL(ReservaRepositorio repositorio)
        {
            _repositorio = repositorio;
        }

        public List<Reserva> ObtenerMisViajes(string nombreUsuario)
        {
            return _repositorio.ObtenerReservasPorPasajero(nombreUsuario);
        }
        public Reserva ObtenerDetalleMisViajes(int idReserva, string nombreUsuario)
        {
            return _repositorio.ObtenerReservaPorIdPasajero(idReserva, nombreUsuario);
        }
    }
}
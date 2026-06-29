using SistemaTicoBus.DA.Repositorios;
using SistemaTicoBus.MODEL.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.BL
{
    public class ViajeCanceladoBL
    {
        private readonly ViajeCanceladoRepositorio repositorio;

        public ViajeCanceladoBL(ViajeCanceladoRepositorio repositorio)
        {
            this.repositorio = repositorio;
        }

        public List<ViajeCancelado> ListarViajesCancelados()
        {
            return repositorio.ListarViajesCancelados();
        }

        public ViajeCancelado? ObtenerDetalleViajeCancelado(int numeroViaje)
        {
            return repositorio.ObtenerDetalleViajeCancelado(numeroViaje);
        }
    }
}

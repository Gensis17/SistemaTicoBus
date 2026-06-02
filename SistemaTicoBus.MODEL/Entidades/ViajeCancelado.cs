using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.MODEL.Entidades
{
    public class ViajeCancelado
    {
        public int NumeroViaje { get; set; }

        public string Ruta { get; set; } = string.Empty;

        public string PlacaUnidad { get; set; } = string.Empty;

        public string NombreChofer { get; set; } = string.Empty;

        public DateTime FechaHoraSalida { get; set; }

        public DateTime FechaHoraLlegadaEstimada { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string MotivoCancelacion { get; set; } = string.Empty;
    }
}

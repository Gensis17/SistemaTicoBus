using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaTicoBus.MODEL.Entidades
{
    public class Unidad
    {
        public string Placa { get; set; }

        public string Modelo { get; set; }

        public int AnioFabricacion { get; set; }

        public int CapacidadPasajeros { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaTicoBus.MODEL.Entidades
{
    public class Unidad
    {
        [Key]
        public string Placa { get; set; }

        public string Modelo { get; set; }

        public int AnioFabricacion { get; set; }

        public int CapacidadPasajeros { get; set; }

        public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();

    }
}

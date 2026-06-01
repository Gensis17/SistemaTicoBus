using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTicoBus.MODEL.Entidades
{
    public class Viaje
    {
        [Key]
        public int IdViaje { get; set; }

        [Required]
        public int IdRuta { get; set; }

        [Required]
        public string PlacaUnidad { get; set; } = string.Empty;

        [Required]
        public string ChoferId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha y Hora de Salida")]
        public DateTime FechaHoraSalida { get; set; }

        [Required]
        [Display(Name = "Fecha y Hora Estimada de llegada")]
        public DateTime FechaHoraLlegadaEstimada { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Programado"; //El viaje se crea en estado "Programado" por defecto

        public string? MotivoCancelacion { get; set; }

        // Propiedades de navegación
        [ForeignKey("IdRuta")]
        public virtual Ruta? Ruta { get; set; }

        [ForeignKey("PlacaUnidad")]
        public virtual Unidad? Unidad { get; set; }

        [ForeignKey("ChoferId")]
        public virtual Chofer? Chofer { get; set; }

        // Relación inversa: Un viaje tiene muchas reservas
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}

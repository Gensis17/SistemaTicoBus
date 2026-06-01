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
        public int IdUnidad { get; set; }

        [Required]
        [Display(Name = "Fecha y Hora de Salida")]
        public DateTime FechaHoraSalida { get; set; }

        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "En Curso"; // Valores: "En Curso", "Completado"

        // Propiedades de navegación
        [ForeignKey("IdRuta")]
        public virtual Ruta? Ruta { get; set; }

        [ForeignKey("IdUnidad")]
        public virtual Unidad? Unidad { get; set; }

        // Relación inversa: Un viaje tiene muchas reservas
        public virtual ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}

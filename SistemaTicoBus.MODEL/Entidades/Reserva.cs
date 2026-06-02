using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTicoBus.MODEL.Entidades
{
    public class Reserva
    {
        [Key]
        public int IdReserva { get; set; }

        [Required]
        public int IdViaje { get; set; }

        [Required]
        public string IdPasajero { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Número de Asiento")]
        public int NumeroAsiento { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Monto Pagado")]
        public decimal MontoPagado { get; set; }

        [Required]
        public DateTime FechaReserva { get; set; } = DateTime.Now;

        // Propiedades de navegación
        [ForeignKey("IdViaje")]
        public virtual Viaje? Viaje { get; set; }

        [ForeignKey("IdPasajero")]
        public virtual Pasajero? Pasajero { get; set; }

    }

}

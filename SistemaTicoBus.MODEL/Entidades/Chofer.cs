using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTicoBus.MODEL.Entidades
{
    public class Chofer
    {
        [Key]
        public string Identificacion { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Apellidos { get; set; } = string.Empty;

        public int UsuarioId { get; set; }

        public virtual ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();

    }
}

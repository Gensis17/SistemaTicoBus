using System.ComponentModel.DataAnnotations;

namespace SistemaTicoBus.WEB.Models
{
    public class ChoferViewModel
    {
        [Required(ErrorMessage = "La cédula es requerida.")]
        [RegularExpression(@"^[0-9\-]+$", ErrorMessage = "La cédula solo puede contener números y guiones.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "La cédula debe tener entre 6 y 20 caracteres.")]
        public string Identificacion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü ]+$", ErrorMessage = "El nombre solo puede contener letras y espacios.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü ]+$", ErrorMessage = "Los apellidos solo pueden contener letras y espacios.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 50 caracteres.")]
        public string Apellidos { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es requerido.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        public string Correo { get; set; } = string.Empty;

        public string? NombreUsuario { get; set; }

        public string? ClaveGenerada { get; set; }
    }
}
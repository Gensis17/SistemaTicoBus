using System.ComponentModel.DataAnnotations;

namespace SistemaTicoBus.WEB.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede superar los 50 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La clave actual es requerida.")]
        [StringLength(255, ErrorMessage = "La clave actual no puede superar los 255 caracteres.")]
        [Display(Name = "Clave actual")]
        public string ClaveActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva clave es requerida.")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La nueva clave debe tener entre 6 y 255 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "La nueva clave debe contener letras y números.")]
        [Display(Name = "Nueva clave")]
        public string NuevaClave { get; set; } = string.Empty;
    }
}
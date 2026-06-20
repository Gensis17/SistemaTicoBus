using System.ComponentModel.DataAnnotations;

namespace SistemaTicoBus.WEB.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede superar los 50 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [StringLength(255, ErrorMessage = "La contraseña no puede superar los 255 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
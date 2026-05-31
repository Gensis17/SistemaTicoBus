namespace SistemaTicoBus.BL.Servicios
{
    public interface IEmailServicio
    {
        Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo);
    }
}
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SistemaTicoBus.BL.Servicios
{
    public class EmailServicio : IEmailServicio
    {
        private readonly EmailSettings _emailSettings;

        public EmailServicio(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                throw new ArgumentException("El destinatario del correo es requerido.");
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.SmtpServer))
            {
                throw new InvalidOperationException("El servidor SMTP no está configurado.");
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
            {
                throw new InvalidOperationException("El correo remitente no está configurado.");
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.Username) ||
                string.IsNullOrWhiteSpace(_emailSettings.Password) ||
                _emailSettings.Username.Contains("COLOCAR") ||
                _emailSettings.Password.Contains("COLOCAR"))
            {
                throw new InvalidOperationException("Las credenciales SMTP no están configuradas.");
            }

            MimeMessage mensaje = new MimeMessage();

            mensaje.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;

            mensaje.Body = new TextPart("plain")
            {
                Text = cuerpo
            };

            using SmtpClient cliente = new SmtpClient();

            SecureSocketOptions seguridad = _emailSettings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await cliente.ConnectAsync(
                _emailSettings.SmtpServer,
                _emailSettings.Port,
                seguridad
            );

            await cliente.AuthenticateAsync(
                _emailSettings.Username,
                _emailSettings.Password
            );

            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);
        }
    }
}
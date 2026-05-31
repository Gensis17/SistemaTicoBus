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
            MimeMessage mensaje = new MimeMessage();

            mensaje.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            mensaje.To.Add(MailboxAddress.Parse(destinatario));
            mensaje.Subject = asunto;

            mensaje.Body = new TextPart("plain")
            {
                Text = cuerpo
            };

            using (SmtpClient cliente = new SmtpClient())
            {
                await cliente.ConnectAsync(
                    _emailSettings.SmtpServer,
                    _emailSettings.Port,
                    SecureSocketOptions.StartTls
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
}

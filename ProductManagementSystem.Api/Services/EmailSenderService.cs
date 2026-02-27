using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ProductManagementSystem.Api.Services;

public class EmailSenderService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSenderService> _logger;

    public EmailSenderService(IConfiguration config, ILogger<EmailSenderService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Envia um e-mail autenticado usando SMTP do Google.
    /// A senha deve ser injetada via appsettings.json ou Variáveis de Ambiente por boas práticas.
    /// Exemplo no appsettings.json:
    /// "SmtpConfig": {
    ///     "Password": "abcdefghijklmnop" // App Password gerado no Google Account (16 caracteres)
    /// }
    /// </summary>
    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        // 1. Configuracoes do SMTP do Google (porta 587, host smtp.gmail.com)
        var smtpHost = "smtp.gmail.com";
        var smtpPort = 587;
        var senderEmail = "storenextbox@gmail.com";
        
        // 2. Nunca deixe a senha "hardcoded" no codigo fonte!
        // Idealmente voce pega isso do _config["SmtpConfig:Password"] ou Environment Variables
        // Assim, se o codigo for pro GitHub, sua senha estara segura.
        var appPassword = _config["SmtpConfig:Password"] ?? Environment.GetEnvironmentVariable("GOOGLE_APP_PASSWORD");

        if (string.IsNullOrEmpty(appPassword))
        {
            _logger.LogWarning("Falha no email: Senha de App SMTP não configurada no appsettings.json ou 'GOOGLE_APP_PASSWORD' var.");
            return false;
        }

        try
        {
            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(senderEmail, appPassword),
                EnableSsl = true, // Exigencia do Gmail para TLS
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "NexBox Store"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true // Permite o recebimento do design "laranjinha" renderizado!
            };

            mailMessage.To.Add(to);

            // 3. Envio Assincrono (não trava o app)
            await smtpClient.SendMailAsync(mailMessage);
            
            _logger.LogInformation("E-mail com comprovante enviado com sucesso para {Destinatario}", to);
            return true;
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "Erro de autenticação ou envio no servidor SMTP.");
            return false;
        }
        catch (Exception ex)
        {
            // Tratamento de exceções robusto: se a internet cair, o bloco Try/Catch impede o travamento fatal.
            _logger.LogError(ex, "Falha de rede ou sistema ao tentar enviar e-mail para {Destinatario}", to);
            return false;
        }
    }
}

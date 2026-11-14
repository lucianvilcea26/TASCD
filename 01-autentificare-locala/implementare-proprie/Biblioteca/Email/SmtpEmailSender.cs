using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Biblioteca.Email;

public class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1026;
    public string From { get; set; } = "";
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body);
}

// Trimite emailuri text simplu prin SMTP. În laborator, serverul este Mailpit, care le afișează fără să le livreze.
public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        var smtp = options.Value;
        using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = false };
        using var message = new MailMessage(smtp.From, to, subject, body);
        await client.SendMailAsync(message);
        logger.LogInformation("Email trimis către {To}: {Subject}", to, subject);
    }
}

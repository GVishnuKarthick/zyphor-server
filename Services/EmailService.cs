using MailKit.Net.Smtp;
using MimeKit;

namespace ZyphorAPI.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

   public async Task SendOtpEmail(string toEmail, string otp)
{
    try
    {
        Console.WriteLine($"Sending OTP to: {toEmail}");
        Console.WriteLine($"OTP: {otp}");

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config["EmailSettings:SenderEmail"]));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Zyphor Email Verification OTP";

        email.Body = new TextPart("plain")
        {
            Text = $"Your OTP is: {otp}"
        };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            "smtp.gmail.com",
            465,
            MailKit.Security.SecureSocketOptions.SslOnConnect
        );

        await smtp.AuthenticateAsync(
            _config["EmailSettings:SenderEmail"],
            _config["EmailSettings:AppPassword"]
        );

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);

        Console.WriteLine("Email sent successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("EMAIL ERROR: " + ex.Message);
        throw;
    }
}
}
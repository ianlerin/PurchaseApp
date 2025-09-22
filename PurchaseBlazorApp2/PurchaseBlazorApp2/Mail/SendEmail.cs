using System.Net.Mail;

namespace PurchaseBlazorApp2.Mail
{
    public class SendEmail
    {
        void SendEmailViaSmtp(string to, string subject, string body)
        {
            using var smtp = new SmtpClient("smtp.yourserver.com")
            {
                Port = 587,
                Credentials = new System.Net.NetworkCredential("user", "password"),
                EnableSsl = true
            };

            var mail = new MailMessage("noreply@yourapp.com", to, subject, body);
            smtp.Send(mail);
        }
    }
}

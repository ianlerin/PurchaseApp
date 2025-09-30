using Microsoft.Graph.Models;
using Microsoft.Graph;

namespace PurchaseBlazorApp2.Service
{
    public class EmailService
    {
        private readonly GraphServiceClient _graph;
        public EmailService(GraphServiceClient graph)
        {
            _graph = graph;
        }
        private readonly string _senderEmail = "KhengHoekTeoh@GenesisSolution272.onmicrosoft.com";
        public async Task SendEmailAsync(
            List<string> toRecipients,
            List<string>? ccRecipients,
            string subject,
            string body,
            bool isHtml = false,
            bool saveToSentItems = true)
        {
            if (toRecipients == null || toRecipients.Count == 0)
                throw new ArgumentException("At least one recipient must be specified.", nameof(toRecipients));

            try
            {
                Console.WriteLine("📧 Preparing to send email...");
                Console.WriteLine($"   To: {string.Join(", ", toRecipients)}");
                if (ccRecipients != null)
                    Console.WriteLine($"   Cc: {string.Join(", ", ccRecipients)}");

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = isHtml ? BodyType.Html : BodyType.Text,
                        Content = body
                    },
                    ToRecipients = ConvertToRecipients(toRecipients)
                };

                // Only add CC if there are recipients
                if (ccRecipients != null && ccRecipients.Any())
                {
                    message.CcRecipients = ConvertToRecipients(ccRecipients);
                }

                var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = saveToSentItems
                };

                await _graph.Users[_senderEmail].SendMail.PostAsync(requestBody);

                Console.WriteLine($"✅ Email sent successfully to: {string.Join(", ", toRecipients)}");
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"❌ Graph API error: {ex.Message}");
                Console.WriteLine($"Status Code: {ex.ResponseStatusCode}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                throw; // rethrow for higher-level handling
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ General error: {ex.Message}");
                throw; // rethrow for higher-level handling
            }
        }

        private static List<Recipient> ConvertToRecipients(List<string> emails)
        {
            var recipients = new List<Recipient>();
            foreach (var email in emails)
            {
                recipients.Add(new Recipient
                {
                    EmailAddress = new EmailAddress { Address = email }
                });
            }
            return recipients;
        }

    }
}

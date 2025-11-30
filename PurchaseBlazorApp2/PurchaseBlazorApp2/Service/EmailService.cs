using Microsoft.Graph;
using Microsoft.Graph.Models;
using PurchaseBlazorApp2.Components.Data;

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
            List<EmailAttachment>? attachments = null,
            bool isHtml = false,
            bool saveToSentItems = true)
        {
            if (toRecipients == null || toRecipients.Count == 0)
                throw new ArgumentException("At least one recipient must be specified.", nameof(toRecipients));

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = isHtml ? BodyType.Html : BodyType.Text,
                    Content = body
                },
                ToRecipients = ConvertToRecipients(toRecipients),
            };

            if (ccRecipients != null && ccRecipients.Any())
                message.CcRecipients = ConvertToRecipients(ccRecipients);

            if (attachments != null && attachments.Any())
            {
                message.HasAttachments = true;

                var fileAttachments = new List<Attachment>();
                foreach (var att in attachments)
                {
                    fileAttachments.Add(new FileAttachment
                    {
                        OdataType = "#microsoft.graph.fileAttachment",
                        Name = att.FileName,
                        ContentBytes = Convert.FromBase64String(att.Base64Content)
                    });
                }

                message.Attachments = fileAttachments;
            }

            var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
            {
                Message = message,
                SaveToSentItems = saveToSentItems
            };

            await _graph.Users[_senderEmail].SendMail.PostAsync(requestBody);
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

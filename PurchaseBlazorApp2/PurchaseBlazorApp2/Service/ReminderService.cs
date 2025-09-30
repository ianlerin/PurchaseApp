using Microsoft.Graph.Models;
using Microsoft.Graph;
using PurchaseBlazorApp2.Components.Repository;
using Microsoft.AspNetCore.Components;
using PurchaseBlazorApp2.Components.Data;
using ServiceStack;
using System.Text;

namespace PurchaseBlazorApp2.Service
{
    public class ReminderEmailService : BackgroundService
    {

        private readonly IServiceProvider _serviceProvider;

        private EmailService _emailService;
        private readonly GraphServiceClient _graph;

        public ReminderEmailService(GraphServiceClient graph, IServiceProvider serviceProvider)
        {
            _graph = graph;
          
            _serviceProvider = serviceProvider;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
         

         
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            _emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GatherandSendReminder();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending reminder: {ex.Message}");
                }

                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task GatherandSendReminder()
        {
            PRRepository PRRepo = new PRRepository();
            List<string> ToRemind=await PRRepo.GetPendingRemindersAsync(1);
            CredentialRepo CredentialRepository = new CredentialRepo();
            List<EDepartment> Departments = new List<EDepartment> { EDepartment.ProcurementManager};
            List<string> Emails=await CredentialRepository.TryGetAllProcurementEmail(Departments);
            await PRRepo.MarkRemindersAsSentAsync(ToRemind);
            await SendApprovalEmailAsync(Emails, null, ToRemind, "https://localhost:7129");
        }

        public async Task SendApprovalEmailAsync(
            List<string> toRecipients,
            List<string>? ccRecipients,
            List<string> requisitionNumbers,
            string baseUrl)
        {
            if (requisitionNumbers == null || requisitionNumbers.Count == 0)
                return;

            // Build the HTML with multiple buttons
            var buttonsHtml = new StringBuilder();

            foreach (var reqNo in requisitionNumbers)
            {
                var encodedReturnUrl = Uri.EscapeDataString($"purchaserequisitionrecords_client/create/{reqNo}");
                var requisitionUrl = $"{baseUrl}/authentication/login?returnUrl={encodedReturnUrl}";

                buttonsHtml.Append($@"
            <div style='margin-bottom:15px;'>
                <p>Purchase Requisition: <strong>{reqNo}</strong></p>
                <a href='{requisitionUrl}'
                   style='background-color:#007bff; color:#ffffff; padding:10px 20px;
                          text-decoration:none; border-radius:5px; display:inline-block;'>
                    Approve Requisition
                </a>
            </div>");
            }

            var emailBody = $@"
        <html>
            <body style='font-family:Segoe UI, Arial, sans-serif; color:#333; font-size:14px;'>
                <p>Hello,</p>
                <p>The following purchase requisitions require your review and approval:</p>
                {buttonsHtml}
                <p>If the buttons above don't work, copy and paste the links into your browser.</p>
                <p>Thank you,<br/>Procurement Team</p>
            </body>
        </html>";

            // Use your existing EmailService
            await _emailService.SendEmailAsync(
                toRecipients,
                ccRecipients,
                $"Approval Needed: {requisitionNumbers.Count} Purchase Requisition(s)",
                emailBody,
                isHtml: true
            );
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PurchaseBlazorApp2.Components.Data;

namespace Genesis.EmailService
{
    public class EmailWorkflowService
    {
        private readonly HttpClient _http;
        private readonly NavigationManager _navigation;
        private readonly IJSRuntime _js;

        public EmailWorkflowService(HttpClient http, NavigationManager navigation, IJSRuntime js)
        {
            _http = http;
            _navigation = navigation;
            _js = js;
        }

        public string EmailSendStatus { get; private set; } = string.Empty;

        /// <summary>
        /// Sends PR-related notification emails to the appropriate parties.
        /// </summary>
        public async Task SendEmailToRelevantPartyAsync(PurchaseRequisitionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            record.OnApprovalChanged();

            List<string> ccList = new();
            List<string> mainList = new();
            string? currentEmail = await GetCurrentEmailAsync();

            switch (record.approvalstatus)
            {
                case EApprovalStatus.PreApproval:
                    await _js.InvokeVoidAsync("console.log", "Status: PreApproval");
                    mainList = await GetProcurementEmailsAsync();
                    if (!string.IsNullOrWhiteSpace(currentEmail))
                        ccList.Add(currentEmail);
                    await SendPreApprovalEmailAsync(record, mainList, ccList);
                    break;

                case EApprovalStatus.PendingApproval:
                    await _js.InvokeVoidAsync("console.log", "Status: PendingApproval");
                    mainList = await GetAllNeededForApprovalEmailsAsync(record);
                    ccList = await GetProcurementEmailsAsync();
                    await SendEmailAsync(record, mainList, ccList);
                    break;

                case EApprovalStatus.Approved:
                case EApprovalStatus.Rejected:
                    await _js.InvokeVoidAsync("console.log", $"Status: {record.approvalstatus}");
                    mainList = await GetProcurementEmailsAsync();
                    if (!string.IsNullOrWhiteSpace(currentEmail))
                        ccList.Add(currentEmail);
                    await SendEmailAsync(record, mainList, ccList);
                    break;
            }

           
        }

        public async Task SendEmailToRelevantPartyFinance(PurchaseOrderRecord PORecord)
        {
            if (PORecord == null)
                throw new ArgumentNullException(nameof(PORecord));


            List<string> ccList = new();
            List<string> mainList = new();
            string? currentEmail = await GetCurrentEmailAsync();

            switch (PORecord.InvoiceInfo.PaymentStatus)
            {
                case (EPaymentStatus.PendingInvoice):
                mainList = await GetFinanceEmailsAsync();
                await SendPaymentReadyEmailAsync(PORecord.PO_ID, mainList, ccList);
                break;
                case (EPaymentStatus.Paid):
                    mainList = await GetProcurementEmailsAsync();
                    await SendFinanceApprovalNotificationAsync(PORecord.PO_ID, mainList, ccList);
                    break;
            }
       }

        private async Task SendFinanceApprovalNotificationAsync(string POID, List<string> mainEmailList, List<string> ccList)
        {
            if (mainEmailList == null || !mainEmailList.Any(e => !string.IsNullOrWhiteSpace(e)))
            {
                EmailSendStatus = "⚠️ No main recipients, email not sent.";
                Console.WriteLine(EmailSendStatus);
                return;
            }

            // Log for debug
            string mainEmails = string.Join(", ", mainEmailList);
            string ccEmails = string.Join(", ", ccList);
            await _js.InvokeVoidAsync("console.log", $"Main Emails: {mainEmails}");
            await _js.InvokeVoidAsync("console.log", $"CC Emails: {ccEmails}");

            string encodedReturnUrl = Uri.EscapeDataString($"finance-record/{POID}");
            string approvalUrl = $"{_navigation.BaseUri}authentication/login?returnUrl={encodedReturnUrl}";

            var emailRequest = new EmailRequest
            {
                To = mainEmailList,
                Cc = ccList,
                Subject = $"Finance Approval Completed: Purchase Order #{POID}",
                Body = $@"
        <html>
            <body style='font-family:Segoe UI, Arial, sans-serif; color:#333; font-size:14px;'>
                <p>Hello Purchase Manager,</p>
                <p>The finance department has reviewed and approved the payment for 
                purchase order <strong>{POID}</strong>.</p>
                <p style='margin:20px 0;'>
                    <a href='{approvalUrl}'
                       style='background-color:#007bff; color:#ffffff; padding:10px 20px;
                              text-decoration:none; border-radius:5px; display:inline-block;'>
                        View Purchase Order
                    </a>
                </p>
                <p>If the button above doesn't work, you can access it directly using this link:</p>
                <p><a href='{approvalUrl}'>{approvalUrl}</a></p>
                <p>Thank you,<br/>Finance Department</p>
            </body>
        </html>",
                IsHtml = true
            };

            try
            {
                var response = await _http.PostAsJsonAsync("api/email/send", emailRequest);

                if (response.IsSuccessStatusCode)
                {
                    EmailSendStatus = $"✅ Finance approval email sent successfully!\nTo: {mainEmails}\nCc: {ccEmails}";
                    Console.WriteLine(EmailSendStatus);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    EmailSendStatus = $"❌ Failed to send finance approval email: {response.StatusCode}";
                    Console.WriteLine($"❌ {EmailSendStatus}: {error}");
                }
            }
            catch (Exception ex)
            {
                EmailSendStatus = $"❌ Exception sending finance approval email: {ex.Message}";
                Console.WriteLine(EmailSendStatus);
            }
        }

        private async Task SendPaymentReadyEmailAsync(string POID, List<string> mainEmailList, List<string> ccList)
        {
            if (mainEmailList == null || !mainEmailList.Any(e => !string.IsNullOrWhiteSpace(e)))
            {
                EmailSendStatus = "⚠️ No main recipients, email not sent.";
                Console.WriteLine(EmailSendStatus);
                return;
            }

            // Log for debug
            string mainEmails = string.Join(", ", mainEmailList);
            string ccEmails = string.Join(", ", ccList);
            await _js.InvokeVoidAsync("console.log", $"Main Emails: {mainEmails}");
            await _js.InvokeVoidAsync("console.log", $"CC Emails: {ccEmails}");

            string encodedReturnUrl = Uri.EscapeDataString($"finance-record/{POID}");
            string paymentUrl = $"{_navigation.BaseUri}authentication/login?returnUrl={encodedReturnUrl}";

            var emailRequest = new EmailRequest
            {
                To = mainEmailList,
                Cc = ccList,
                Subject = $"Payment Ready for Processing: Purchase Order #{POID}",
                Body = $@"
            <html>
                <body style='font-family:Segoe UI, Arial, sans-serif; color:#333; font-size:14px;'>
                    <p>Hello Finance Department,</p>
                    <p>A purchase order (<strong>{POID}</strong>) has been fully approved and is ready for payment processing.</p>
                    <p style='margin:20px 0;'>
                        <a href='{paymentUrl}'
                           style='background-color:#28a745; color:#ffffff; padding:10px 20px;
                                  text-decoration:none; border-radius:5px; display:inline-block;'>
                            Review and Process Payment
                        </a>
                    </p>
                    <p>If the button above doesn't work, copy and paste this link:</p>
                    <p><a href='{paymentUrl}'>{paymentUrl}</a></p>
                    <p>Thank you,<br/>Procurement System</p>
                </body>
            </html>",
                IsHtml = true
            };

            try
            {
                var response = await _http.PostAsJsonAsync("api/email/send", emailRequest);

                if (response.IsSuccessStatusCode)
                {
                    EmailSendStatus = $"✅ Email sent successfully!\nTo: {mainEmails}\nCc: {ccEmails}";
                    Console.WriteLine(EmailSendStatus);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    EmailSendStatus = $"❌ Failed to send email: {response.StatusCode}";
                    Console.WriteLine($"❌ {EmailSendStatus}: {error}");
                }
            }
            catch (Exception ex)
            {
                EmailSendStatus = $"❌ Exception sending email: {ex.Message}";
                Console.WriteLine(EmailSendStatus);
            }
        }
        /// <summary>
        /// Sends a pre approval email with a generated link for approval.
        /// </summary>
        private async Task SendPreApprovalEmailAsync(PurchaseRequisitionRecord record, List<string> mainEmailList, List<string> ccList)
        {
            if (mainEmailList == null || !mainEmailList.Any(e => !string.IsNullOrWhiteSpace(e)))
            {
                EmailSendStatus = "⚠️ No main recipients, email not sent.";
                Console.WriteLine(EmailSendStatus);
                return;
            }

            // Log for debug
            string mainEmails = string.Join(", ", mainEmailList);
            string ccEmails = string.Join(", ", ccList);
            await _js.InvokeVoidAsync("console.log", $"Main Emails: {mainEmails}");
            await _js.InvokeVoidAsync("console.log", $"CC Emails: {ccEmails}");

            string encodedReturnUrl = Uri.EscapeDataString(
                $"pr-preapproval/{record.RequisitionNumber}");

            string requisitionUrl =
                $"{_navigation.BaseUri}authentication/login?returnUrl={encodedReturnUrl}";

            var emailRequest = new EmailRequest
            {
                To = mainEmailList,
                Cc = ccList,
                Subject = $"New PR Created: Purchase Requisition #{record.RequisitionNumber}",
                Body = $@"
                    <html>
                        <body style='font-family:Segoe UI, Arial, sans-serif; color:#333; font-size:14px;'>
                            <p>Hello,</p>
                            <p>A new purchase requisition (<strong>{record.RequisitionNumber}</strong>) requires your review.</p>
                            <p style='margin:20px 0;'>
                                <a href='{requisitionUrl}'
                                   style='background-color:#007bff; color:#ffffff; padding:10px 20px;
                                          text-decoration:none; border-radius:5px; display:inline-block;'>
                                    Approve Requisition
                                </a>
                            </p>
                            <p>If the button above doesn't work, copy and paste this link:</p>
                            <p><a href='{requisitionUrl}'>{requisitionUrl}</a></p>
                            <p>Thank you,<br/>Procurement Team</p>
                        </body>
                    </html>",
                IsHtml = true
            };

            try
            {
                var response = await _http.PostAsJsonAsync("api/email/send", emailRequest);

                if (response.IsSuccessStatusCode)
                {
                    EmailSendStatus = $"✅ Email sent successfully!\nTo: {mainEmails}\nCc: {ccEmails}";
                    Console.WriteLine(EmailSendStatus);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    EmailSendStatus = $"❌ Failed to send email: {response.StatusCode}";
                    Console.WriteLine($"❌ {EmailSendStatus}: {error}");
                }
            }
            catch (Exception ex)
            {
                EmailSendStatus = $"❌ Exception sending email: {ex.Message}";
                Console.WriteLine(EmailSendStatus);
            }
        }

        /// <summary>
        /// Sends an email with a generated link for approval.
        /// </summary>
        private async Task SendEmailAsync(PurchaseRequisitionRecord record, List<string> mainEmailList, List<string> ccList)
        {
            if (mainEmailList == null || !mainEmailList.Any(e => !string.IsNullOrWhiteSpace(e)))
            {
                EmailSendStatus = "⚠️ No main recipients, email not sent.";
                Console.WriteLine(EmailSendStatus);
                return;
            }

            // Log for debug
            string mainEmails = string.Join(", ", mainEmailList);
            string ccEmails = string.Join(", ", ccList);
            await _js.InvokeVoidAsync("console.log", $"Main Emails: {mainEmails}");
            await _js.InvokeVoidAsync("console.log", $"CC Emails: {ccEmails}");

            string encodedReturnUrl = Uri.EscapeDataString(
                $"purchaserequisitionrecords_client/create/{record.RequisitionNumber}");

            string requisitionUrl =
                $"{_navigation.BaseUri}authentication/login?returnUrl={encodedReturnUrl}";

            var emailRequest = new EmailRequest
            {
                To = mainEmailList,
                Cc = ccList,
                Subject = $"Approval Needed: Purchase Requisition #{record.RequisitionNumber}",
                Body = $@"
                    <html>
                        <body style='font-family:Segoe UI, Arial, sans-serif; color:#333; font-size:14px;'>
                            <p>Hello,</p>
                            <p>A new purchase requisition (<strong>{record.RequisitionNumber}</strong>) requires your review and approval.</p>
                            <p style='margin:20px 0;'>
                                <a href='{requisitionUrl}'
                                   style='background-color:#007bff; color:#ffffff; padding:10px 20px;
                                          text-decoration:none; border-radius:5px; display:inline-block;'>
                                    Approve Requisition
                                </a>
                            </p>
                            <p>If the button above doesn't work, copy and paste this link:</p>
                            <p><a href='{requisitionUrl}'>{requisitionUrl}</a></p>
                            <p>Thank you,<br/>Procurement Team</p>
                        </body>
                    </html>",
                IsHtml = true
            };

            try
            {
                var response = await _http.PostAsJsonAsync("api/email/send", emailRequest);

                if (response.IsSuccessStatusCode)
                {
                    EmailSendStatus = $"✅ Email sent successfully!\nTo: {mainEmails}\nCc: {ccEmails}";
                    Console.WriteLine(EmailSendStatus);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    EmailSendStatus = $"❌ Failed to send email: {response.StatusCode}";
                    Console.WriteLine($"❌ {EmailSendStatus}: {error}");
                }
            }
            catch (Exception ex)
            {
                EmailSendStatus = $"❌ Exception sending email: {ex.Message}";
                Console.WriteLine(EmailSendStatus);
            }
        }

        /// <summary>
        /// Retrieves all approval department emails for this PR.
        /// </summary>
        private async Task<List<string>> GetAllNeededForApprovalEmailsAsync(PurchaseRequisitionRecord record)
        {
            List<string> emailList = new();
            if (record == null)
                return emailList;

            var departments = record.GetSelectedDepartments();
            var response = await _http.PostAsJsonAsync(
                _navigation.ToAbsoluteUri("api/login/getrolemail"),
                departments.ToList());

            if (response.IsSuccessStatusCode)
            {
                emailList = await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
            }

            return emailList;
        }

        /// <summary>
        /// Retrieves finance emails.
        /// </summary>
        private async Task<List<string>> GetFinanceEmailsAsync()
        {
            var response = await _http.PostAsJsonAsync(
                _navigation.ToAbsoluteUri("api/login/getrolemail"),
                new List<EDepartment> { EDepartment.Finance });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
            }

            return new List<string>();
        }

        /// <summary>
        /// Retrieves procurement manager emails.
        /// </summary>
        private async Task<List<string>> GetProcurementEmailsAsync()
        {
            var response = await _http.PostAsJsonAsync(
                _navigation.ToAbsoluteUri("api/login/getrolemail"),
                new List<EDepartment> { EDepartment.ProcurementManager });

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
            }

            return new List<string>();
        }

        /// <summary>
        /// (Optional) Retrieves current logged-in user's email.
        /// </summary>
        private async Task<string> GetCurrentEmailAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/login/currentemail");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Service;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/email")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;

        public EmailController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            
            if (request == null || request.To == null || request.To.Count == 0)
                return BadRequest("Recipient list cannot be empty.");

            await _emailService.SendEmailAsync(
                toRecipients: request.To,
                ccRecipients: request.Cc,
                subject: request.Subject,
                body: request.Body,
                isHtml: request.IsHtml
            );
            

            return Ok("✅ Email sent successfully.");
        }
    }
}

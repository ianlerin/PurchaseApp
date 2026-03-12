using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/finance")]
    [ApiController]
    public class FinanceController : ControllerBase
    {
        [HttpPost("submit")]
        public async Task<ActionResult<bool>> SubmitAsync([FromBody] FinanceRecord Info)
        {
            string companyId = HttpContext.Request.Headers["CompanyId"].ToString();

            FinanceRepository financeRepository = new FinanceRepository();  
            bool bSubmitResult = await financeRepository.Submit(Info,companyId);
            return Ok(bSubmitResult);

        }
        [HttpGet("get")]
        public async Task<ActionResult<FinanceRecord>> GetFinanceRecord([FromQuery] string requisitionNumber)
        {
            FinanceRepository financeRepository = new FinanceRepository();
            FinanceRecord record = await financeRepository.GetFinanceRecordByRequisitionNumber(requisitionNumber);
            return Ok(record);
        }
    }
}

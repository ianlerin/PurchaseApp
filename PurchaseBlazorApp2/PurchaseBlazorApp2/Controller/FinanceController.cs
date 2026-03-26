using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/finance")]
    [ApiController]
    public class FinanceController : ControllerBase
    {
        int MyCompanyID = 0;

        public FinanceController()
        {
           
        }
        private async Task<FinanceRepository> GetMyRepo()
        {
            int.TryParse(Request.Headers["CompanyID"], out MyCompanyID);
            CredentialRepo CredentialRepo = new CredentialRepo();
            string DBName = await CredentialRepo.TryGetDatabaseNameByCompanyId(MyCompanyID);
            FinanceRepository MyRepo = new FinanceRepository(DBName);
            return MyRepo;

        }
        [HttpPost("submit")]
        public async Task<ActionResult<bool>> SubmitAsync([FromBody] FinanceRecord Info)
        {
            FinanceRepository financeRepository = await GetMyRepo();  
            bool bSubmitResult = await financeRepository.Submit(Info);
            return Ok(bSubmitResult);

        }
        [HttpGet("get")]
        public async Task<ActionResult<FinanceRecord>> GetFinanceRecord([FromQuery] string requisitionNumber)
        {
            FinanceRepository financeRepository = await GetMyRepo();
            FinanceRecord record = await financeRepository.GetFinanceRecordByRequisitionNumber(requisitionNumber);
            return Ok(record);
        }
    }
}

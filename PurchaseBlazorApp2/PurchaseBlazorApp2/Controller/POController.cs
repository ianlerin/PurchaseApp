using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/po")]
    [ApiController]
    public class POController : ControllerBase
    {
        int MyCompanyID = 0;
        public POController()
        {
        }
        private async Task<PORepository> GetMyRepo()
        {
            int.TryParse(Request.Headers["CompanyID"], out MyCompanyID);
            CredentialRepo CredentialRepo = new CredentialRepo();
            string DBName = await CredentialRepo.TryGetDatabaseNameByCompanyId(MyCompanyID);
            PORepository MyRepo = new PORepository(DBName);
            return MyRepo;

        }

        [HttpPost("get")]
        public async Task<ActionResult<List<PurchaseOrderRecord>>> GetRecordsAsync([FromBody] List<string> requisitionNumbers)
        {
            PORepository PORepository= await GetMyRepo();
            var allRecords = await PORepository.GetRecordsAsync(requisitionNumbers);
            return Ok(allRecords);

        }

        [HttpPost("submit")]
        public async Task<ActionResult<POSubmitResponse>> SubmitAsync([FromBody] IEnumerable<PurchaseOrderRecord> InfoList)
        {
            PORepository PORepository = await GetMyRepo();
            var allRecords = await PORepository.SubmitAsync(InfoList);
            return Ok(allRecords);

        }

        [HttpPost("get_pr")]
        public async Task<ActionResult<List<PurchaseOrderRecord>>> GetRecordsAsyncWithPR([FromBody] List<string> requisitionNumbers)
        {
            PORepository PORepository = await GetMyRepo();
            var allRecords = await PORepository.GetRecordsAsyncWithPR(requisitionNumbers);
            return Ok(allRecords);

        }
        [HttpGet("get_deliverydate")]
        public async Task<ActionResult<List<DateTime>>> GetDeliveryDate([FromQuery] List<string> requisitionNumbers)
        {
            PORepository PORepository = await GetMyRepo();
            List<DateTime> allRecords = await PORepository.GetDeliveryDatesAsync(requisitionNumbers);
            return Ok(allRecords);
        }
    }
}

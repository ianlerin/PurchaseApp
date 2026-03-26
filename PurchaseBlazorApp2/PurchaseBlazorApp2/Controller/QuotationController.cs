using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;
using static PurchaseBlazorApp2.Client.Pages.Quotation.QuotationInfo;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/quotation")]
    [ApiController]
    public class QuotationController : ControllerBase
    {
        int MyCompanyID = 0;
        QuotationRepo QuotationRepository { get; set; }

        private async Task<QuotationRepo> GetMyRepo()
        {
            int.TryParse(Request.Headers["CompanyID"], out MyCompanyID);
            CredentialRepo CredentialRepo = new CredentialRepo();
            string DBName = await CredentialRepo.TryGetDatabaseNameByCompanyId(MyCompanyID);
            QuotationRepo MyRepo = new QuotationRepo(DBName);
            return MyRepo;

        }

        public QuotationController()
        {
           
        }

        [HttpPost("submit")]
        public async Task<ActionResult<bool>> SubmitPRs([FromBody] IEnumerable<QuotationRecord> infoList)
        {
            QuotationRepository = await GetMyRepo();
            bool Result = await QuotationRepository.SubmitAsync(infoList);
            return Ok(Result);
        }

        [HttpPost("get")]
        public async Task<ActionResult<List<QuotationRecord>>> GetPrs([FromBody] List<string> infoList)
        {
            QuotationRepository = await GetMyRepo();
            List<QuotationRecord> Result = await QuotationRepository.GetRecordsForListAsync(infoList);
            return Ok(Result);
        }

    }
}

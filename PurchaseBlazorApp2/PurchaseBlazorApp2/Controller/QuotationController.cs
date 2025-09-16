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
        QuotationRepo QuotationRepository { get; set; }
        public QuotationController()
        {
            QuotationRepository = new QuotationRepo();
        }

        [HttpPost("submit")]
        public async Task<ActionResult<bool>> SubmitPRs([FromBody] IEnumerable<QuotationRecord> infoList)
        {
            bool Result = await QuotationRepository.SubmitAsync(infoList);
            return Ok(Result);
        }

        [HttpPost("get")]
        public async Task<ActionResult<List<QuotationRecord>>> GetPrs([FromBody] List<string> infoList)
        {
            List<QuotationRecord> Result = await QuotationRepository.GetRecordsForListAsync(infoList);
            return Ok(Result);
        }

    }
}

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
        PORepository PORepository { get; set; }
        public POController()
        {
            PORepository = new PORepository();
        }
        [HttpPost("get")]
        public async Task<ActionResult<List<PurchaseOrderRecord>>> GetRecordsAsync([FromBody] List<string> requisitionNumbers)
        {
            var allRecords = await PORepository.GetRecordsAsync(requisitionNumbers);
            return Ok(allRecords);

        }

        [HttpPost("submit")]
        public async Task<ActionResult<List<PurchaseOrderRecord>>> SubmitAsync([FromBody] IEnumerable<PurchaseOrderRecord> InfoList)
        {
            var allRecords = await PORepository.SubmitAsync(InfoList);
            return Ok(allRecords);

        }

    }
}

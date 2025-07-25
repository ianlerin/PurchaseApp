using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;
using ServiceStack.DataAnnotations;

namespace PurchaseBlazorApp2.Components
{
    [Route("api/pr")]
    [ApiController]
    public class PRController : ControllerBase
    {
        PRRepository PRRepository { get; set; }
        public PRController()
        {
            PRRepository= new PRRepository();
        }

        [HttpPost("submit")]
        public async Task<ActionResult<bool>> SubmitPRs([FromBody] IEnumerable<PurchaseRequisitionRecord> infoList)
        {
           bool Result= await  PRRepository.SubmitAsync(infoList);
           return Ok(Result);
        }

        [HttpPost("insert-approval")]
        public async Task<ActionResult<List<ApprovalInfo>>> InsertApprovalByRequisitionNumber([FromBody] PurchaseRequisitionRecord record)
        {
            var result = await PRRepository.InsertApprovalByRequisitionNumber(record);
            return Ok(result);
        }

    }
}

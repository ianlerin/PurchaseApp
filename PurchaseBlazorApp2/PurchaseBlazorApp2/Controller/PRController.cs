using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;
using ServiceStack.DataAnnotations;

namespace PurchaseBlazorApp2.Controller
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
        public async Task<ActionResult<List<string>>> SubmitPRs([FromBody] IEnumerable<PurchaseRequisitionRecord> infoList)
        {
            List<string> Result = await PRRepository.SubmitAsync(infoList);
           return Ok(Result);
        }

        [HttpPost("insert-approval")]
        public async Task<ActionResult<List<ApprovalInfo>>> InsertApprovalByRequisitionNumber([FromBody] PurchaseRequisitionRecord record)
        {
            var result = await PRRepository.InsertApprovalByRequisitionNumber(record);
            return Ok(result);
        }

        [HttpPost("edit-deliverydate")]
        public async Task<ActionResult<bool>> InsertDeliveryDate([FromBody] DeliveryDateUpdateRequest Request)
        {
            var result = await PRRepository.UpdateDeliveryDateAsync(Request.PR_ID, Request.DeliveryDate);
            return Ok(result);
        }

        [HttpPost("get-list-partial")]
        public async Task<ActionResult<List<PurchaseRequisitionRecord>>> GetRecordsForListAsync([FromBody] List<string> requisitionNumbers)
        {
            var Result = await PRRepository.GetAllRecordsForListAsync(requisitionNumbers);
            return Ok(Result);
        }

        [HttpPost("get-detail")]
        public async Task<ActionResult<List<PurchaseRequisitionRecord>>> GetRecordsAsync([FromBody] List<string> requisitionNumbers)
        {
            var Result = await PRRepository.GetRecordsAsync(requisitionNumbers);
            return Ok(Result);
        }

        [HttpPost("get-needapproval")]
        public async Task<ActionResult<HashSet<string>>> GetRecordsAsync([FromBody]EDepartment Department)
        {
            var Result = await PRRepository.GetRequisitionNumbersByDepartmentAsync(Department);
            return Ok(Result);
        }

        [HttpPost("get-createdby")]
        public async Task<ActionResult<HashSet<string>>> GetRecordsAsync([FromBody] string CreatedBy)
        {
            var Result = await PRRepository.GetRequisitionNumbersByCreatedByAsync(CreatedBy);
            return Ok(Result);
        }

        [HttpPost("get-finance")]
        public async Task<ActionResult<HashSet<string>>> GetRecordsFinance()
        {
            var Result = await PRRepository.GetRequisitionsFinance();
            return Ok(Result);
        }

        [HttpPost("get-list-partial-all")]
        public async Task<ActionResult<List<PurchaseRequisitionRecord>>> GetRecordsForListAsync([FromBody] EPRSearchStatus Status)
        {
            List<PurchaseRequisitionRecord> Result;
            if(Status==EPRSearchStatus.Full)
            {
                Result = await PRRepository.GetAllRecordsForListAsync();
            }
            else
            {
                Result = await PRRepository.GetPartialRecordsForListAsync();
            }
            return Result;
        }
        

    }
}

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
            List<string> result = await PRRepository.SubmitAsync(infoList, null);
            return Ok(result);
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
        public async Task<ActionResult<List<PurchaseRequisitionRecord>>> GetRecordsForListAsync([FromBody] PRListRequest request)
        {
            var Result = await PRRepository.GetAllRecordsForListAsync(request.CompanyId, request.RequisitionNumbers);
            return Ok(Result);
        }

        [HttpPost("get-detail")]
        public async Task<ActionResult<List<PurchaseRequisitionRecord>>> GetRecordsAsync([FromBody] List<string> requisitionNumbers)
        {
            string companyId = HttpContext.Request.Headers["CompanyId"].ToString();

            var Result = await PRRepository.GetRecordsAsync(companyId, requisitionNumbers);
            return Ok(Result);
        }

        [HttpPost("get-needapproval")]
        public async Task<ActionResult<HashSet<string>>> GetRecordsNeedApproval([FromBody] ApprovalRequest request)
        {
            var result = await PRRepository.GetRequisitionNumbersByDepartmentAsync(request.Department, request.CompanyId);
            return Ok(result);
        }

        [HttpPost("get-createdby")]
        public async Task<ActionResult<HashSet<string>>> GetRecordsCreatedBy([FromBody] CreatedByRequest request)
        {
            var result = await PRRepository.GetRequisitionNumbersByCreatedByAsync(request.Email, request.CompanyId);
            return Ok(result);
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

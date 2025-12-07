using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;
using PurchaseBlazorApp2.Service;
using WorkerRecord;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/pdf")]
    [ApiController]
    public class PDFController : ControllerBase
    {
        [HttpGet("purchase/{poId}")]
        public async Task<IActionResult> GeneratePurchasePdf(string poId)
        {
            // Fetch your PO from DB based on poId
            PORepository Repo = new PORepository();
            List<PurchaseOrderRecord>Records=await Repo.GetRecordsAsync(new List<string> { poId });

            if (Records.Count==0) return NotFound();
            var PO = Records[0];
            if (PO == null) return NotFound();

            PRRepository PRRepo = new PRRepository();
            List<RequestItemInfo> RequestedItems = await PRRepo.GetRequestedItemByRequisitionNumber(PO.PR_ID, "pr_approved_requestitem_table");

            var pdfBytes = new POPDFHelper().GeneratePurchaseOrderPdf(PO, RequestedItems);
            return File(pdfBytes, "application/pdf", $"PurchaseOrder-{PO.PO_ID}.pdf");
        }
        [HttpGet("purchase")]
        public async Task<IActionResult> GenerateWagesPDF(int year,int month)
        {
            // Fetch your PO from DB based on poId
            HRRepository Repo = new HRRepository();
            WageRecord Record = await Repo.GetWageRecordAsync(year,month);

            if (Record.WageRecords.Count == 0) return NotFound();
          
            var pdfBytes = new HRPDFHelper().GenerateWagePdf(Record);
            return File(pdfBytes, "application/pdf", $"Wages-{Record.Year}.{Record.Month}.pdf");
        }
    }
}

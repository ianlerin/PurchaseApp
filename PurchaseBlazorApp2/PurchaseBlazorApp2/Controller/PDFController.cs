using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;

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
            List<RequestItemInfo> RequestedItems = await PRRepo.GetRequestedItemByRequisitionNumber(PO.PR_ID);

            var pdfBytes = new POPDFHelper().GeneratePurchaseOrderPdf(PO, RequestedItems);
            return File(pdfBytes, "application/pdf", $"PurchaseOrder-{PO.PO_ID}.pdf");
        }
    }
}

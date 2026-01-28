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
            List<PurchaseOrderRecord> Records = await Repo.GetRecordsAsync(new List<string> { poId });

            if (Records.Count == 0) return NotFound();
            var PO = Records[0];
            if (PO == null) return NotFound();

            PRRepository PRRepo = new PRRepository();
            List<RequestItemInfo> RequestedItems = await PRRepo.GetRequestedItemByRequisitionNumber(PO.PR_ID, "pr_approved_requestitem_table");

            var pdfBytes = new POPDFHelper().GeneratePurchaseOrderPdf(PO, RequestedItems);
            return File(pdfBytes, "application/pdf", $"PurchaseOrder-{PO.PO_ID}.pdf");
        }
        [HttpPost("purchase")]
        public async Task<IActionResult> GenerateWagesPDF(GenerateWagesPdfRequest WagesRequest)
        {
            // Fetch your PO from DB based on poId
            HRRepository Repo = new HRRepository();
            WageRecord Record = await Repo.GetWageRecordAsync(WagesRequest.Year, WagesRequest.Month);

            if (Record.WageRecords.Count == 0) return NotFound();
            var pdfBytes = new HRPDFHelper().GenerateWagePdf(Record, WagesRequest.MyUser);
            return File(pdfBytes, "application/pdf", $"Wages-{Record.Year}.{Record.Month}.pdf");
        }

        [HttpGet("hr/workers")]
        public async Task<IActionResult> GenerateWorkerPdf([FromQuery] string status = "All", [FromQuery] string nationality = "All")
        {
            HRRepository repo = new HRRepository();
            List<WorkerRecord.WorkerRecord> workers;

            if (!Enum.TryParse<EWorkerStatus>(status, true, out var parsedStatus))
                parsedStatus = EWorkerStatus.All;

            if (!Enum.TryParse<ENationalityStatus>(nationality, true, out var parsedNationality))
                parsedNationality = ENationalityStatus.Local;

            if (parsedStatus == EWorkerStatus.All)
            {
                var active = await repo.GetWorkersByStatus(EWorkerStatus.Active);
                var inactive = await repo.GetWorkersByStatus(EWorkerStatus.Inactive);
                workers = active.Concat(inactive).ToList();
            }
            else
            {
                workers = await repo.GetWorkersByStatus(parsedStatus);
            }
            if (!string.Equals(nationality, "All", StringComparison.OrdinalIgnoreCase))
            {
                workers = workers.Where(w => w.NationalityStatus == parsedNationality).ToList();
            }


            if (workers.Count == 0)
                return NotFound("No workers found");

            var pdfBytes = new WorkerPDFHelper()
                .GenerateWorkerPdf(workers, parsedStatus);

            return File(
                pdfBytes,
                "application/pdf",
                $"Workers-{parsedStatus}.pdf"
            );
        }

        [HttpPost("slip")]
        public async Task<IActionResult> GenerateSlip([FromBody] SingleWageRecord record)
        {
            var pdfBytes = new SlipPDFHelper().GeneratePaymentSlip(record);
            return File(pdfBytes, "application/pdf", $"PaymentSlip-{record.Name}.pdf");
        }
        [HttpPost("Combineslip")]
        public async Task<string> CombineSlip([FromBody] List<string> Slips)
        {
            var SingleBase64 = new SlipPDFHelper().CombinePdfBase64(Slips);
            return SingleBase64;
        }
    }
}

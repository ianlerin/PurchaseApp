using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Repository;
using WorkerRecord;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/HR")]
    [ApiController]
    public class HRController : ControllerBase
    {
        private readonly HRRepository _repo;

        public HRController()
        {
            _repo = new HRRepository();
        }
        [HttpPost("SubmitWorker")]
        public async Task<IActionResult> SubmitWorker([FromBody] WorkerRecord.WorkerRecord worker)
        {
            if (worker == null)
                return BadRequest("Worker record is empty.");

            bool ok = await _repo.Submit(worker);

            if (ok)
                return Ok(new { message = "Worker saved successfully." });

            return StatusCode(500, new { message = "Failed to save worker." });
        }

        [HttpGet("GetWorkersByStatus/{status}")]
        public async Task<IActionResult> GetWorkersByStatus(string status)
        {
            if (!Enum.TryParse<EWorkerStatus>(status, true, out var parsedStatus))
                return BadRequest(new { message = "Invalid worker status." });

            var workers = await _repo.GetWorkersByStatus(parsedStatus);

            return Ok(workers);
        }

        [HttpPost("InsertWagesInfo")]
        public IActionResult InsertWageRecord([FromBody] WageRecord WageInfo)
        {
            if (WageInfo.WageRecords == null || WageInfo.WageRecords == null || WageInfo.WageRecords.Count == 0)
            {
                return BadRequest("WageRecord data is empty or null.");
            }

            try
            {
                _repo.InsertWageRecord(WageInfo.Year, WageInfo.Month, WageInfo);
                return Ok(new { Message = "Wage records inserted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error inserting wage records.", Details = ex.Message });
            }
        }
    }
}

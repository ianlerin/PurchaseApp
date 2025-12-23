using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Repository;
using PurchaseBlazorApp2.Resource;
using WorkerRecord;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/HR")]
    [ApiController]
    public class HRController : ControllerBase
    {
        private readonly HRRepository _repo;
        private readonly IEPFTableService _epfService;
        public HRController(IEPFTableService epfService)
        {
            _repo = new HRRepository();
            _epfService = epfService;
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


        [HttpGet("GetWagesInfo")]
        public async Task<IActionResult> GetWagesInfo([FromQuery] int year, [FromQuery] int month)
        {
            try
            {

                var data = await _repo.GetWageRecordAsync(year, month);

                if (data == null || data.WageRecords.Count == 0)
                    return NotFound(new { Message = "No wage records found." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error retrieving wage info.", Details = ex.Message });

            }
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

        [HttpGet("GetAllWorkers")]
        public async Task<IActionResult> GetAllWorkers([FromQuery] EWorkerStatus status = EWorkerStatus.All)
        {
            if (status == EWorkerStatus.All)
            {
                var activeWorkers = await _repo.GetWorkersByStatus(EWorkerStatus.Active);
                var inactiveWorkers = await _repo.GetWorkersByStatus(EWorkerStatus.Inactive);

                return Ok(activeWorkers.Concat(inactiveWorkers).ToList());
            }

            var workers = await _repo.GetWorkersByStatus(status);
            return Ok(workers);
        }

        [HttpPost("calculateEPF")]
        public IActionResult Calculate([FromBody] SingleWageRecord record)
        {
            if (record == null)
                return BadRequest("Record is null.");

            var result = _epfService.UpdateWageInfo(record);
            return Ok(new
            {
                EmployerContribution = result.Employer,
                EmployeeContribution = result.Employee
            });
        }
    }
}

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
        int MyCompanyID = 0;
     
        private readonly IEPFTableService _epfService;
        public HRController(IEPFTableService epfService)
        {
            _epfService = epfService;
        }

        private async Task<HRRepository> GetMyRepo()
        {
            int.TryParse(Request.Headers["CompanyID"], out MyCompanyID);
            CredentialRepo CredentialRepo = new CredentialRepo();
            string DBName = await CredentialRepo.TryGetDatabaseNameByCompanyId(MyCompanyID);
            HRRepository MyRepo = new HRRepository(DBName);
            return MyRepo;

        }

        [HttpPost("SubmitWorker")]
        public async Task<IActionResult> SubmitWorker([FromBody] List<WorkerRecord.WorkerRecord> workers
)
        {
            HRRepository _repo= await GetMyRepo();
            if (workers == null)
                return BadRequest("Worker record is empty.");

            bool ok = await _repo.Submit(workers);

            if (ok)
                return Ok(new { message = "Worker saved successfully." });

            return StatusCode(500, new { message = "Failed to save worker." });
        }

        [HttpGet("GetWorkersByStatus/{status}")]
        public async Task<IActionResult> GetWorkersByStatus(string status)
        {
            HRRepository _repo = await GetMyRepo();
            if (!Enum.TryParse<EWorkerStatus>(status, true, out var parsedStatus))
                return BadRequest(new { message = "Invalid worker status." });

            var workers = await _repo.GetWorkersByStatus(parsedStatus);

            return Ok(workers);
        }


        [HttpGet("GetWagesInfo")]
        public async Task<IActionResult> GetWagesInfo([FromQuery] int year, [FromQuery] int month)
        {
            HRRepository _repo = await GetMyRepo();
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
        public async Task<IActionResult> InsertWageRecord([FromBody] WageRecord WageInfo)
        {
            HRRepository _repo = await GetMyRepo();
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
            HRRepository _repo = await GetMyRepo();
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
        public async Task<ActionResult<ContributeResult>> Calculate([FromBody] SingleWageRecord record)
        {
            HRRepository _repo = await GetMyRepo();
            if (record == null)
                return BadRequest("Record is null.");

            var (EPFemployer, EPFemployee) = _epfService.UpdateEPFWageInfo(record);
            var (Socsoemployer, Socsoemployee) = _epfService.UpdateSocsoWageInfo(record);
            var result = new ContributeResult
            {
                EPFEmployerContribution = EPFemployer,
                EPFEmployeeContribution = EPFemployee,
                SocsoEmployerContribution = Socsoemployer,
                SocsoEmployeeContribution = Socsoemployer,

            };

            return Ok(result);
        }

        [HttpGet("GetWorkerById")]
        public async Task<WorkerRecord.WorkerRecord> GetWorkerById(string id)
        {
            HRRepository _repo = await GetMyRepo();
            var workers = await _repo.GetWorkersByStatus(EWorkerStatus.Active);
            return workers.FirstOrDefault(w => w.ID == id);
        }


    }
}

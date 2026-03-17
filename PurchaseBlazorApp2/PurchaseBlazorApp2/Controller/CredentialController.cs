using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/login")]
    [ApiController]
    public class CredentialController : ControllerBase
    {
        CredentialRepo CredentialRepository { get; set; }
        public CredentialController()
        {
            CredentialRepository = new CredentialRepo();
        }

        [HttpPost("submit")]
        public async Task<ActionResult<CredentialSubmitResponse>> SubmitPRs([FromBody] UserName info)
        {
            CredentialSubmitResponse FoundUserName = await CredentialRepository.TryLoginAsync(info);
            return Ok(FoundUserName);
        }

        [HttpPost("register")]
        public async Task<ActionResult<bool>> Register([FromBody] UserName info)
        {
            bool bSuccess = await CredentialRepository.RegisterAsync(info);
            return Ok(bSuccess);
        }

        [HttpPost("getrole")]
        public async Task<ActionResult<EDepartment>> GetRole([FromBody] string  email)
        {
            EDepartment Department = await CredentialRepository.TryGetRole(email);
            return Ok(Department);
        }
        [HttpPost("gethrrole")]
        public async Task<ActionResult<EHRRole>> GetHRRole([FromBody] string email)
        {
            EHRRole Department = await CredentialRepository.TryGetHRRole(email);
            return Ok(Department);
        }
        [HttpGet("checkexist/{email}")]
        public async Task<ActionResult<bool>> IfExist(string email)
        {
            bool exists = await CredentialRepository.CheckIfUserExistsAsync(email);
            return Ok(exists);
        }


        [HttpPost("getrolemail")]
        public async Task<ActionResult<List<string>>> TryGetAllProcurementEmail([FromBody] List<EDepartment> Departments)
        {
            List<string> Emails = await CredentialRepository.TryGetAllProcurementEmail(Departments);
            return Ok(Emails);
        }

        [HttpPost("getavailablecompanies")]
        public async Task<ActionResult<List<CompanyInfo>>> TryGetAllAvailableCompanies([FromBody] string userId)
        {
            List<CompanyInfo> CompanyInfo = await CredentialRepository.TryGetAllCompanyInfo(userId);
            return Ok(CompanyInfo);
        }

    }
}

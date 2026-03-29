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
        [HttpPost("getuserid")]
        public async Task<ActionResult<int>> GetUserID([FromBody] string username)
        {
            int ID = await CredentialRepository.GetUserID(username);
            return Ok(ID);
        }
        [HttpPost("getrole")]
        public async Task<ActionResult<EDepartment>> GetRole([FromBody] GetRoleRequest Request)
        {
            EDepartment Department = await CredentialRepository.TryGetRole(Request.UserID, Request.CompanyId);
            return Ok(Department);
        }
        [HttpPost("gethrrole")]
        public async Task<ActionResult<EHRRole>> GetHRRole([FromBody] GetRoleRequest Request)
        {
            EHRRole Department = await CredentialRepository.TryGetHRRole(Request.UserID, Request.CompanyId);
            return Ok(Department);
        }
        [HttpGet("checkexist/{username}")]
        public async Task<ActionResult<bool>> IfExist(string username)
        {
            bool bExist = await CredentialRepository.CheckIfUsernameExistsAsync(username);
            return Ok(bExist);
        }


        [HttpPost("getrolemail")]
        public async Task<ActionResult<List<string>>> TryGetAllProcurementEmail([FromBody] DepartmentInfo DepartmentInfo)
        {
            List<string> Emails = await CredentialRepository.TryGetAllProcurementEmail(DepartmentInfo);
            return Ok(Emails);
        }

        [HttpPost("getavailablecompanies")]
        public async Task<ActionResult<List<CompanyInfo>>> TryGetAllAvailableCompanies([FromBody] int UserID)
        {
            List<CompanyInfo> CompanyInfo = await CredentialRepository.TryGetAllCompanyInfo(UserID);
            return Ok(CompanyInfo);
        }
        [HttpPost("getcompany")]
        public async Task<ActionResult<CompanyInfo>> TryGetCompanyInfo([FromBody] CompanyInquireInfo InquireInfo)
        {
           CompanyInfo CompanyInfo = await CredentialRepository.TryGetCompanyInfo(InquireInfo.UserID,InquireInfo.CompanyId);
            return Ok(CompanyInfo);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Global;
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
        public async Task<ActionResult<bool>> SubmitPRs([FromBody] UserName info)
        {
            bool Result = await CredentialRepository.TryLoginAsync(info);
            return Ok(Result);
        }
    }
}

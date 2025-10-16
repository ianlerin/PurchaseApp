using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Components.Repository;
using SharedDataType;
using static PurchaseBlazorApp2.Client.Pages.Quotation.QuotationInfo;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/supplier")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        [HttpPost("submit")]
        public async Task<ActionResult<bool>> SubmitSupplier([FromBody] IEnumerable<SupplierRecord> infoList)
        {

            SupplierRepository Repo=new SupplierRepository();
            bool bSubmit= await Repo.SubmitAsync(infoList);
            return Ok(bSubmit);
        }

        [HttpGet("getall")]
        public async Task<ActionResult<List<SupplierRecord>>> GetAllSuppliers()
        {

            SupplierRepository Repo = new SupplierRepository();
            List<SupplierRecord> Suppliers = await Repo.GetAllSuppliersAsync();
            return Ok(Suppliers);
        }

        [HttpGet("getallname")]
        public async Task<ActionResult<List<SupplierLookUpInfo>>> GetAllSupplierName()
        {

            SupplierRepository Repo = new SupplierRepository();
            List<SupplierLookUpInfo> SupplierNames = await Repo.AsyncGetAllSupplierName();
            return Ok(SupplierNames);
        }

        [HttpGet("get/{sid}")]
        public async Task<ActionResult<SupplierRecord>> GetSupplierById(string sid)
        {
            SupplierRepository repo = new SupplierRepository();
            var supplier = await repo.GetSupplierByIdAsync(sid);

            if (supplier == null)
            {
                return NotFound();
            }

            return Ok(supplier);
        }

    }
}

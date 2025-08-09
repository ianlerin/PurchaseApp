using Microsoft.AspNetCore.Mvc;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/table/pr")]
    [ApiController]
    public class PrTableController : TableController
    {
        /*
        public PrTableController() : base(
            new List<KeyValuePair<string, List<string>>>
            {
                new KeyValuePair<string, List<string>>(
                    "prtable",
                    new List<string>
                    {
                        "requisitionnumber",
                        "requestor",
                        "prstatus",
                        "purpose"
                    })
            })
        {
            return;
        }
        */
    }
}

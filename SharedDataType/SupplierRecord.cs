using PurchaseBlazorApp2.Components.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedDataType
{
    public class SupplierRecord
    {
        [Key]
        public string? SID { get; set; }
        public string? companyname { get; set; }
        public string? contactperson { get; set; }
        public string? contact { get; set; }
        public string? email { get; set; }

        //ship to detail
        public string? shiptocompanyname { get; set; }
        public string? warehouseaddress { get; set; }
        public string? receivingperson { get; set; }
        public string? shippingcontact { get; set; }

    }
}

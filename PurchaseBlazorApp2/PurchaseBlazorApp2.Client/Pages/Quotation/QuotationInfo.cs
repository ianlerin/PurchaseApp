using PurchaseBlazorApp2.Components.Data;
using System.ComponentModel.DataAnnotations;

namespace PurchaseBlazorApp2.Client.Pages.Quotation
{
    public class QuotationInfo
    {
        public class QuotationRecord
        {
            [Key]
            public string? quotation_id { get; set; }
            public string? pr_id { get; set; }


            public List<ImageUploadInfo> SupportDocuments { get; set; } = new List<ImageUploadInfo>();

            public byte[]? selectedid { get; set; }
        }


        }
    }

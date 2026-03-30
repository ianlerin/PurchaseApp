using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryRecord
{
    public enum InventoryStatus
    {
        Active,
        Inactive
    }
    public class InventoryItemData
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public int Quantity { get; set; }

        public string SKUCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Flavour { get; set; } = "";
        public string PackSize { get; set; } = "";      
        public decimal CostPerUnit { get; set; }
        public decimal B2BPrice { get; set; }
        public decimal B2CPrice { get; set; }
        public string CartonConfiguration { get; set; } = "";

        public InventoryStatus Status { get; set; } = InventoryStatus.Active;


    }
    public class InventorySupplierData
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public int Quantity { get; set; }
        public string SupplierName { get; set; } = "";
        public string ContactDetails { get; set; } = "";
        public string PaymentTerms { get; set; } = "";
    }

    public class InventoryCustomerData
    {
        public string ID { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Phone { get; set; } = "";  
        public string Address { get; set; } = "";
        public string PaymentTerms { get; set; } = "";
        public string CreditLimit { get; set; } 
    }
    public class InventoryRecordData
    {
        public InventoryItemData ItemData { get; set; } = new();
        public InventorySupplierData SupplierData { get; set; } = new();
        public int Quantity { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class IdResponse
    {
        public string Id { get; set; } = "";
    }
    internal class InventoryRecord
    {
          
    }
}

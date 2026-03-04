using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryRecord
{
    public class InventoryItemData
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
    }
    public class InventorySupplierData
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public int Quantity { get; set; }
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

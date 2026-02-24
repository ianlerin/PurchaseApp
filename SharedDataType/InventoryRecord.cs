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
    }
    public class InventorySupplierData
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public string? Contact { get; set; }
    }

    public class InventoryRecordData
    {
        public InventoryItemData ItemData = new InventoryItemData();
        public InventorySupplierData SupplierData = new InventorySupplierData();
        public int Quantity = 0;
        public string CreatedBy = "";
    }
    public class IdResponse
    {
        public string Id { get; set; } = "";
    }
    internal class InventoryRecord
    {
        
    }
}

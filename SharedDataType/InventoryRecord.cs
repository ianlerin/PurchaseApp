using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryRecord
{
    public class InventoryItemData
    {
        public string ID = "";
        public string Name = "";
    }
    public class InventorySupplierData
    {
        public string ID = "";
        public string Name = "";
        public string? Address;
        public string? Contact;
    }

    public class InventoryRecordData
    {
        public InventoryItemData ItemData = new InventoryItemData();
        public InventorySupplierData SupplierData = new InventorySupplierData();
        public int Quantity = 0;
        public string CreatedBy = "";
    }
    internal class InventoryRecord
    {
        
    }
}

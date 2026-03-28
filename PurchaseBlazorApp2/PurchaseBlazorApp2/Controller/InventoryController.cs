using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Repository;
using InventoryRecord;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        int MyCompanyID = 0;
        string MyDB = "";
        public InventoryController()
        {
           
        }


        private async Task<InventoryRepository> GetMyRepo()
        {
            int.TryParse(Request.Headers["CompanyID"], out MyCompanyID);
            CredentialRepo CredentialRepo = new CredentialRepo();
            string DBName = await CredentialRepo.TryGetDatabaseNameByCompanyId(MyCompanyID);
            InventoryRepository MyRepo = new InventoryRepository(DBName);
            return MyRepo;

        }

        [HttpPost("add-supplier")]
        public async Task<IActionResult> AddSupplier([FromBody] InventorySupplierData supplier)
        {
            InventoryRepository _repo = await GetMyRepo();
            if (supplier == null)
                return BadRequest("Supplier data cannot be null");
            if (string.IsNullOrWhiteSpace(supplier.Name))
                return BadRequest("Supplier Name is required");

            try
            {
                var newId = await _repo.AddSupplierAsync(supplier);
                return Ok(new { id = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding supplier: {ex.Message}");
            }
        }

        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromBody] InventoryItemData product)
        {
            InventoryRepository _repo = await GetMyRepo();
            if (product == null)
                return BadRequest("Product data cannot be null");
            if (string.IsNullOrWhiteSpace(product.Name))
                return BadRequest("Product Name is required");

            try
            {
                var newId = await _repo.AddProductAsync(product);
                return Ok(new { id = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding product: {ex.Message}");
            }
        }

        [HttpGet("get-suppliers")]
        public async Task<List<InventorySupplierData>> GetSuppliers()
        {
            InventoryRepository _repo = await GetMyRepo();
            return await _repo.GetSuppliersAsync();
        }

        [HttpGet("get-products")]
        public async Task<List<InventoryItemData>> GetProducts()
        {
            InventoryRepository _repo = await GetMyRepo();
            return await _repo.GetProductsAsync();
        }

        [HttpPost("add-record")]
        public async Task<IActionResult> AddRecord([FromBody] InventoryRecordData record)
        {
            InventoryRepository _repo = await GetMyRepo();
            if (record == null)
                return BadRequest("Invalid record");

            await _repo.AddRecordAsync(record);
            return Ok(new { message = "Record saved successfully!" });
        }
        [HttpGet("get-quantity")]
        public async Task<int> GetQuantity(string? productId, string? supplierId)
        {
            InventoryRepository _repo = await GetMyRepo();
            if (!string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(supplierId))
                return await _repo.GetQuantityAsync(productId, supplierId);

            if (!string.IsNullOrEmpty(productId))
                return await _repo.GetProductQuantityAsync(productId);

            if (!string.IsNullOrEmpty(supplierId))
                return await _repo.GetSupplierQuantityAsync(supplierId);

            return 0;
        }

        [HttpGet("get-records")]
        public async Task<List<InventoryRecordData>> GetRecords(string productId)
        {
            InventoryRepository _repo = await GetMyRepo();
            if (string.IsNullOrEmpty(productId))
                return new List<InventoryRecordData>();

            return await _repo.GetRecordsByProductAsync(productId);
        }

        [HttpGet("get-customers")]
        public async Task<List<InventoryCustomerData>> GetCustomers()
        {
            return await _repo.GetCustomersAsync();
        }

        [HttpPost("add-customer")]
        public async Task<IActionResult> AddCustomer([FromBody] InventoryCustomerData customer)
        {
            if (customer == null)
                return BadRequest("Customer data cannot be null");

            if (string.IsNullOrWhiteSpace(customer.CompanyName))
                return BadRequest("Company Name is required");

            try
            {
                var newId = await _repo.AddCustomerAsync(customer);
                return Ok(new { id = newId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding customer: {ex.Message}");
            }
        }
    }
}
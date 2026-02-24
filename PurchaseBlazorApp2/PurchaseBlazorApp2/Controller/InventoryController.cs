using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components.Repository;
using InventoryRecord;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryRepository _repo;

        public InventoryController()
        {
            _repo = new InventoryRepository();
        }

        [HttpPost("add-supplier")]
        public async Task<IActionResult> AddSupplier([FromBody] InventorySupplierData supplier)
        {
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
    }
}
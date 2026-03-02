using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using InventoryRecord;
using Npgsql;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Resource;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class InventoryRepository
    {
        private NpgsqlConnection Connection;

        public InventoryRepository()
        {
            Connection = GetConnection();
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection($"Server={StaticResources.ConnectionId};Port=5432;User Id=postgres;Password=password;Database=purchase");
        }

        private async Task EnsureSequenceAsync(string sequenceName, string tableName, string columnName)
        {
            //Check sequence exists
            var checkCmd = new NpgsqlCommand(
                $"SELECT COUNT(*) FROM information_schema.sequences WHERE sequence_name = '{sequenceName}'",
                Connection);
            var count = (long)await checkCmd.ExecuteScalarAsync();

            if (count == 0)
            {
                var createCmd = new NpgsqlCommand($"CREATE SEQUENCE {sequenceName} START 1;", Connection);
                await createCmd.ExecuteNonQueryAsync();
            }

            var setValCmd = new NpgsqlCommand(
              $@"SELECT setval(
               '{sequenceName}',
                COALESCE(
                (SELECT MAX(CAST(SUBSTRING({columnName} FROM '[0-9]+') AS BIGINT)) 
                FROM {tableName}),
                 0
              ),
              true
            )",
            Connection);

            await setValCmd.ExecuteNonQueryAsync();
        }

        public async Task<string> AddSupplierAsync(InventorySupplierData supplier)
        {
            await Connection.OpenAsync();
            try
            {
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM addsupplier WHERE id=@id", Connection);
                checkCmd.Parameters.AddWithValue("id", supplier.ID ?? "");
                var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;

                if (exists)
                {
                    // update
                    var updateCmd = new NpgsqlCommand(
                        "UPDATE addsupplier SET name=@name, address=@address, contact=@contact WHERE id=@id",
                        Connection);
                    updateCmd.Parameters.AddWithValue("id", supplier.ID);
                    updateCmd.Parameters.AddWithValue("name", supplier.Name ?? "");
                    updateCmd.Parameters.AddWithValue("address", supplier.Address ?? "");
                    updateCmd.Parameters.AddWithValue("contact", supplier.Contact ?? "");
                    await updateCmd.ExecuteNonQueryAsync();

                    return supplier.ID;
                }
                else
                {
                    //insert
                    await EnsureSequenceAsync("addsupplier_seq", "addsupplier", "id");

                    var seqCmd = new NpgsqlCommand("SELECT nextval('addsupplier_seq')", Connection);
                    var seq = (long)await seqCmd.ExecuteScalarAsync();
                    supplier.ID = $"Supplier_{seq}";


                    var insertCmd = new NpgsqlCommand(
                        "INSERT INTO addsupplier (id, name, address, contact) VALUES (@id, @name, @address, @contact)",
                        Connection);
                    insertCmd.Parameters.AddWithValue("id", supplier.ID);
                    insertCmd.Parameters.AddWithValue("name", supplier.Name ?? "");
                    insertCmd.Parameters.AddWithValue("address", supplier.Address ?? "");
                    insertCmd.Parameters.AddWithValue("contact", supplier.Contact ?? "");
                    await insertCmd.ExecuteNonQueryAsync();

                    return supplier.ID;
                }
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task<string> AddProductAsync(InventoryItemData product)
        {
            await Connection.OpenAsync();
            try
            {
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM addproduct WHERE id=@id", Connection);
                checkCmd.Parameters.AddWithValue("id", product.ID ?? "");
                var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;

                if (exists)
                {
                    // update
                    var updateCmd = new NpgsqlCommand(
                        "UPDATE addproduct SET name=@name WHERE id=@id",
                        Connection);
                    updateCmd.Parameters.AddWithValue("id", product.ID);
                    updateCmd.Parameters.AddWithValue("name", product.Name ?? "");
                    await updateCmd.ExecuteNonQueryAsync();

                    return product.ID;
                }
                else
                {
                    //insert
                    await EnsureSequenceAsync("addproduct_seq", "addproduct", "id");

                    var seqCmd = new NpgsqlCommand("SELECT nextval('addproduct_seq')", Connection);
                    var seq = (long)await seqCmd.ExecuteScalarAsync();
                    product.ID = $"Product_{seq}";


                    var insertCmd = new NpgsqlCommand(
                        "INSERT INTO addproduct (id, name) VALUES (@id, @name)",
                        Connection);
                    insertCmd.Parameters.AddWithValue("id", product.ID);
                    insertCmd.Parameters.AddWithValue("name", product.Name ?? "");
                    await insertCmd.ExecuteNonQueryAsync();

                    return product.ID;

                }
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task<List<InventorySupplierData>> GetSuppliersAsync()
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand("SELECT id, name, address, contact FROM addsupplier ORDER BY id", Connection);
                var list = new List<InventorySupplierData>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new InventorySupplierData
                    {
                        ID = reader.GetString(0),
                        Name = reader.GetString(1),
                        Address = reader.GetString(2),
                        Contact = reader.GetString(3)
                    });
                }
                return list;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task<List<InventoryItemData>> GetProductsAsync()
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand("SELECT id, name FROM addproduct ORDER BY id", Connection);
                var list = new List<InventoryItemData>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new InventoryItemData
                    {
                        ID = reader.GetString(0),
                        Name = reader.GetString(1)
                    });
                }
                return list;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task AddRecordAsync(InventoryRecordData record)
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand(@"
              INSERT INTO addrecord (product_id, supplier_id, quantity, created_by)
              VALUES (@product_id, @supplier_id, @quantity, @created_by)
             
            ", Connection);

                cmd.Parameters.AddWithValue("product_id", record.ItemData.ID);
                cmd.Parameters.AddWithValue("supplier_id", record.SupplierData.ID);
                cmd.Parameters.AddWithValue("quantity", record.Quantity);
                cmd.Parameters.AddWithValue("created_by", record.CreatedBy ?? "");

                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task<int> GetSupplierQuantityAsync(string supplierId)
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand(
                    @"SELECT COALESCE(SUM(quantity), 0)
              FROM addrecord
              WHERE supplier_id = @supplier_id",
                    Connection);
                cmd.Parameters.AddWithValue("supplier_id", supplierId);

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task<int> GetProductQuantityAsync(string productId)
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand(
                    @"SELECT COALESCE(SUM(quantity), 0)
              FROM addrecord
              WHERE product_id = @product_id",
                    Connection);
                cmd.Parameters.AddWithValue("product_id", productId);

                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task<List<InventoryRecordData>> GetRecordsByProductAsync(string productId)
        {
            var records = new List<InventoryRecordData>();
            await Connection.OpenAsync();

            try
            {
                var cmd = new NpgsqlCommand(@"
                    SELECT r.product_id, r.supplier_id, r.quantity, r.created_by,
                           p.name as product_name,
                           s.name as supplier_name
                    FROM addrecord r
                    INNER JOIN addproduct p ON p.id = r.product_id
                    INNER JOIN addsupplier s ON s.id = r.supplier_id
                    WHERE r.product_id = @productId
                ", Connection);

                cmd.Parameters.AddWithValue("productId", productId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new InventoryRecordData
                    {
                        ItemData = new InventoryItemData
                        {
                            ID = reader.GetString(0),
                            Name = reader.GetString(4)
                        },
                        SupplierData = new InventorySupplierData
                        {
                            ID = reader.GetString(1),
                            Name = reader.GetString(5)
                        },
                        Quantity = reader.GetInt32(2),
                        
                    });
                }
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return records;
        }

    }
}
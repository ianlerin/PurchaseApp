using DocumentFormat.OpenXml.EMMA;
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
                await EnsureSequenceAsync("addsupplier_seq","addsupplier", "id");

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
    }
}
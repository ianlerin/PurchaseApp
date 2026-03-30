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
        string MyDB = "";
        public InventoryRepository(string DBName)
        {
            MyDB = DBName;
            Connection = GetConnection();
   
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection($"Server={StaticResources.ConnectionId};Port=5432;User Id=postgres;Password=password;Database={MyDB}");
        }

        private async Task EnsureSequenceAsync(string sequenceName, string tableName, string columnName)
        {
            // ✅ Let PostgreSQL handle existence safely
            var createCmd = new NpgsqlCommand(
                $"CREATE SEQUENCE IF NOT EXISTS {sequenceName} START 1;",
                Connection);

            await createCmd.ExecuteNonQueryAsync();

            // ✅ Sync sequence with existing data
            var setValCmd = new NpgsqlCommand(
            $@"SELECT setval(
        '{sequenceName}',
        COALESCE(
            (SELECT MAX(CAST(SUBSTRING({columnName} FROM '[0-9]+') AS BIGINT)) 
             FROM {tableName}),
            0
        ) + 1,
        false
    )",
            Connection);

            await setValCmd.ExecuteNonQueryAsync();
        }
        public async Task<string> AddSupplierAsync(InventorySupplierData supplier)
        {
            await Connection.OpenAsync();
            try
            {
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM inventory.addsupplier WHERE id=@id", Connection);
                checkCmd.Parameters.AddWithValue("id", supplier.ID ?? "");
                var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;

                if (exists)
                {
                    // update
                    var updateCmd = new NpgsqlCommand(
                        "UPDATE inventory.addsupplier SET name=@name, address=@address, contact=@contact, suppliername=@suppliername, contactdetails=@contactdetails, paymentterms=@paymentterms WHERE id=@id",
                        Connection);
                    updateCmd.Parameters.AddWithValue("id", supplier.ID);
                    updateCmd.Parameters.AddWithValue("name", supplier.Name ?? "");
                    updateCmd.Parameters.AddWithValue("address", supplier.Address ?? "");
                    updateCmd.Parameters.AddWithValue("contact", supplier.Contact ?? "");
                    updateCmd.Parameters.AddWithValue("suppliername", supplier.SupplierName ?? "");
                    updateCmd.Parameters.AddWithValue("contactdetails", supplier.ContactDetails ?? "");
                    updateCmd.Parameters.AddWithValue("paymentterms", supplier.PaymentTerms ?? "");
                    await updateCmd.ExecuteNonQueryAsync();

                    return supplier.ID;
                }
                else
                {
                    //insert
                    await EnsureSequenceAsync("inventory.addsupplier_seq", "inventory.addsupplier", "id");

                    var seqCmd = new NpgsqlCommand("SELECT nextval('inventory.addsupplier_seq')", Connection);
                    var seq = (long)await seqCmd.ExecuteScalarAsync();
                    supplier.ID = $"Supplier_{seq}";


                    var insertCmd = new NpgsqlCommand(
                        @"INSERT INTO inventory.addsupplier 
                        (id, name, address, contact,suppliername,contactdetails,paymentterms) 
                        VALUES 
                        (@id, @name, @address, @contact,@suppliername,@contactdetails,@paymentterms)",
                        Connection);
                    insertCmd.Parameters.AddWithValue("id", supplier.ID);
                    insertCmd.Parameters.AddWithValue("name", supplier.Name ?? "");
                    insertCmd.Parameters.AddWithValue("address", supplier.Address ?? "");
                    insertCmd.Parameters.AddWithValue("contact", supplier.Contact ?? "");
                    insertCmd.Parameters.AddWithValue("suppliername", supplier.SupplierName ?? "");
                    insertCmd.Parameters.AddWithValue("contactdetails", supplier.ContactDetails ?? "");
                    insertCmd.Parameters.AddWithValue("paymentterms", supplier.PaymentTerms ?? "");
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
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM inventory.addproduct WHERE id=@id", Connection);
                checkCmd.Parameters.AddWithValue("id", product.ID ?? "");
                var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;

                if (exists)
                {
                    // update
                    var updateCmd = new NpgsqlCommand(
                        "UPDATE inventory.addproduct SET name=@name,skucode=@skucode,productname=@productname,flavour=@flavour,packsize=@packsize,costperunit=@costperunit,b2bprice=@b2bprice ,b2cprice=@b2cprice,cartonconfiguration=@cartonconfiguration, status=@status WHERE id=@id",
                        Connection);
                    updateCmd.Parameters.AddWithValue("id", product.ID);
                    updateCmd.Parameters.AddWithValue("name", product.Name ?? "");
                    updateCmd.Parameters.AddWithValue("skucode", product.SKUCode ?? "");
                    updateCmd.Parameters.AddWithValue("productname", product.ProductName ?? "");
                    updateCmd.Parameters.AddWithValue("flavour", product.Flavour ?? "");
                    updateCmd.Parameters.AddWithValue("packsize", product.PackSize ?? "");
                    updateCmd.Parameters.AddWithValue("costperunit", product.CostPerUnit);
                    updateCmd.Parameters.AddWithValue("b2bprice", product.B2BPrice);
                    updateCmd.Parameters.AddWithValue("b2cprice", product.B2CPrice);
                    updateCmd.Parameters.AddWithValue("cartonconfiguration", product.CartonConfiguration ?? "");
                    updateCmd.Parameters.AddWithValue("status", product.Status.ToString());
                    await updateCmd.ExecuteNonQueryAsync();

                    return product.ID;
                }
                else
                {
                    //insert
                    await EnsureSequenceAsync("inventory.addproduct_seq", "inventory.addproduct", "id");

                    var seqCmd = new NpgsqlCommand("SELECT nextval('inventory.addproduct_seq')", Connection);
                    var seq = (long)await seqCmd.ExecuteScalarAsync();
                    product.ID = $"Product_{seq}";


                    var insertCmd = new NpgsqlCommand(
                        @"INSERT INTO inventory.addproduct
                          (id, name, skucode, productname, flavour, packsize, costperunit, b2bprice, b2cprice, cartonconfiguration, status)
                          VALUES
                          (@id, @name, @skucode, @productname, @flavour, @packsize, @costperunit, @b2bprice, @b2cprice, @cartonconfiguration, @status)",
                        Connection);
                    insertCmd.Parameters.AddWithValue("id", product.ID);
                    insertCmd.Parameters.AddWithValue("name", product.Name ?? "");
                    insertCmd.Parameters.AddWithValue("skucode", product.SKUCode ?? "");
                    insertCmd.Parameters.AddWithValue("productname", product.ProductName ?? "");
                    insertCmd.Parameters.AddWithValue("flavour", product.Flavour ?? "");
                    insertCmd.Parameters.AddWithValue("packsize", product.PackSize ?? "");
                    insertCmd.Parameters.AddWithValue("costperunit", product.CostPerUnit);
                    insertCmd.Parameters.AddWithValue("b2bprice", product.B2BPrice);
                    insertCmd.Parameters.AddWithValue("b2cprice", product.B2CPrice);
                    insertCmd.Parameters.AddWithValue("cartonconfiguration", product.CartonConfiguration ?? "");
                    insertCmd.Parameters.AddWithValue("status", product.Status.ToString());
                    await insertCmd.ExecuteNonQueryAsync();

                    return product.ID;

                }
            }
            catch(Exception Ex)
            {
                Console.WriteLine(Ex);
                return "-1";
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
                var cmd = new NpgsqlCommand("SELECT id, name, address, contact, suppliername,contactdetails,paymentterms FROM inventory.addsupplier ORDER BY id", Connection);
                var list = new List<InventorySupplierData>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new InventorySupplierData
                    {
                        ID = reader.GetString(0),
                        Name = reader.GetString(1),
                        Address = reader.GetString(2),
                        Contact = reader.GetString(3),
                        SupplierName=reader.GetString(4),
                        ContactDetails = reader.GetString(5),
                        PaymentTerms = reader.GetString(6),
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
            List<InventoryItemData> dummy= new List<InventoryItemData>();
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand("SELECT id, name,skucode, productname, flavour, packsize, costperunit, b2bprice, b2cprice, cartonconfiguration, status FROM inventory.addproduct ORDER BY id", Connection);
                var list = new List<InventoryItemData>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new InventoryItemData
                    {
                        ID = reader.GetString(0),
                        Name = reader.GetString(1),
                        SKUCode = reader.GetString(2),
                        ProductName = reader.GetString(3),
                        Flavour = reader.GetString(4),
                        PackSize = reader.GetString(5),
                        CostPerUnit = reader.GetDecimal(6),
                        B2BPrice = reader.GetDecimal(7),
                        B2CPrice = reader.GetDecimal(8),
                        CartonConfiguration = reader.GetString(9),
                        Status = Enum.TryParse<InventoryStatus>(reader.GetString(10), out var status) ? status : InventoryStatus.Active
                    });
                }
                return list;
            }
            catch(Exception Ex)
            {
                Console.WriteLine(Ex.Message);

            }
            finally
            {
                await Connection.CloseAsync();
            }
            return dummy;
        }

        public async Task AddRecordAsync(InventoryRecordData record)
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand(@"
              INSERT INTO inventory.addrecord (product_id, supplier_id, quantity, created_by, created_date)
              VALUES (@product_id, @supplier_id, @quantity, @created_by, NOW())
             
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
              FROM inventory.addrecord
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
              FROM inventory.addrecord
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

        public async Task<int> GetQuantityAsync(string productId, string supplierId)
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand(
                    @"SELECT COALESCE(SUM(quantity), 0)
              FROM inventory.addrecord
              WHERE product_id = @product_id AND supplier_id = @supplier_id",
                    Connection);

                cmd.Parameters.AddWithValue("product_id", productId);
                cmd.Parameters.AddWithValue("supplier_id", supplierId);

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
                    SELECT r.product_id, r.supplier_id, r.quantity, r.created_by, r.created_date,
                           p.name as product_name,
                           s.name as supplier_name
                    FROM inventory.addrecord r
                    INNER JOIN inventory.addproduct p ON p.id = r.product_id
                    INNER JOIN inventory.addsupplier s ON s.id = r.supplier_id
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
                            Name = reader.GetString(5)
                        },
                        SupplierData = new InventorySupplierData
                        {
                            ID = reader.GetString(1),
                            Name = reader.GetString(6)
                        },
                        Quantity = reader.GetInt32(2),
                        CreatedDate = reader.GetDateTime(4),

                    });
                }
            }
            finally
            {
                await Connection.CloseAsync();
            }

            return records;
        }

        public async Task<List<InventoryCustomerData>> GetCustomersAsync()
        {
            await Connection.OpenAsync();
            try
            {
                var cmd = new NpgsqlCommand(
                    @"SELECT id, companyname, contactperson, phone, address, paymentterms, creditlimit 
              FROM inventory.addcustomer ORDER BY id", Connection);

                var list = new List<InventoryCustomerData>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new InventoryCustomerData
                    {
                        ID = reader.GetString(0),
                        CompanyName = reader.GetString(1),
                        ContactPerson = reader.GetString(2),
                        Phone = reader.GetString(3),
                        Address = reader.GetString(4),
                        PaymentTerms = reader.GetString(5),
                        CreditLimit = reader.GetString(6)
                    });
                }

                return list;
            }
            finally
            {
                await Connection.CloseAsync();
            }
        }

        public async Task<string> AddCustomerAsync(InventoryCustomerData customer)
        {
            await Connection.OpenAsync();
            try
            {
                var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM inventory.addcustomer WHERE id=@id", Connection);
                checkCmd.Parameters.AddWithValue("id", customer.ID ?? "");
                var exists = (long)await checkCmd.ExecuteScalarAsync() > 0;

                if (exists)
                {
               
                    var updateCmd = new NpgsqlCommand(
                        @"UPDATE inventory.addcustomer 
                  SET companyname=@companyname,
                      contactperson=@contactperson,
                      phone=@phone,
                      address=@address,
                      paymentterms=@paymentterms,
                      creditlimit=@creditlimit
                  WHERE id=@id", Connection);

                    updateCmd.Parameters.AddWithValue("id", customer.ID);
                    updateCmd.Parameters.AddWithValue("companyname", customer.CompanyName ?? "");
                    updateCmd.Parameters.AddWithValue("contactperson", customer.ContactPerson ?? "");
                    updateCmd.Parameters.AddWithValue("phone", customer.Phone ?? "");
                    updateCmd.Parameters.AddWithValue("address", customer.Address ?? "");
                    updateCmd.Parameters.AddWithValue("paymentterms", customer.PaymentTerms ?? "");
                    updateCmd.Parameters.AddWithValue("creditlimit", customer.CreditLimit);

                    await updateCmd.ExecuteNonQueryAsync();
                    return customer.ID;
                }
                else
                {
                   
                    await EnsureSequenceAsync("inventory.addcustomer_seq", "inventory.addcustomer", "id");

                    var seqCmd = new NpgsqlCommand("SELECT nextval('inventory.addcustomer_seq')", Connection);
                    var seq = (long)await seqCmd.ExecuteScalarAsync();

                    customer.ID = $"Customer_{seq}";

                    var insertCmd = new NpgsqlCommand(
                        @"INSERT INTO inventory.addcustomer 
                  (id, companyname, contactperson, phone, address, paymentterms, creditlimit)
                  VALUES (@id, @companyname, @contactperson, @phone, @address, @paymentterms, @creditlimit)",
                        Connection);

                    insertCmd.Parameters.AddWithValue("id", customer.ID);
                    insertCmd.Parameters.AddWithValue("companyname", customer.CompanyName ?? "");
                    insertCmd.Parameters.AddWithValue("contactperson", customer.ContactPerson ?? "");
                    insertCmd.Parameters.AddWithValue("phone", customer.Phone ?? "");
                    insertCmd.Parameters.AddWithValue("address", customer.Address ?? "");
                    insertCmd.Parameters.AddWithValue("paymentterms", customer.PaymentTerms ?? "");
                    insertCmd.Parameters.AddWithValue("creditlimit", customer.CreditLimit);

                    await insertCmd.ExecuteNonQueryAsync();

                    return customer.ID;
                }
            }
            finally
            {
                await Connection.CloseAsync(); 
            }
        }

    }
}
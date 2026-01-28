using DocumentFormat.OpenXml.Office.Word;
using Npgsql;
using PurchaseBlazorApp2.Client.Pages.HR;
using PurchaseBlazorApp2.Components.Data;
using PurchaseBlazorApp2.Resource;
using ServiceStack.Messaging;
using System.Data.Common;
using WorkerRecord;

namespace PurchaseBlazorApp2.Components.Repository
{
    public class HRRepository
    {
        private NpgsqlConnection Connection;
        public HRRepository()
        {
            Connection = new NpgsqlConnection($"Server={StaticResources.ConnectionId};Port=5432; User Id=postgres; Password=password; Database=purchase");

        }
        private string GetConnectionString()
        {
            return $@"
            Server={StaticResources.ConnectionId};
            Port=5432;
            User Id=postgres;
            Password=password;
            Database=purchase
            ";
        }
       public async Task<bool> Submit(List<WorkerRecord.WorkerRecord> workers)

       {
            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();
                await using var transaction = await conn.BeginTransactionAsync();
                foreach (var worker in workers)
                {
                    string id = worker.ID;

       

                    // ===== Generate ID if new record =====
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        const string seqSql = "SELECT nextval('hr.worker_info_id')";
                        await using var seqCmd = new NpgsqlCommand(seqSql, conn, transaction);

                        var nextVal = await seqCmd.ExecuteScalarAsync();
                        id = $"Worker{nextVal}";
                        worker.ID = id; 
                    }

                    const string sql = @"
            INSERT INTO hr.workerinfo
            (
                id,
                name,
                passport,
                designation_status,
                status,
                recommendation,
                epf_status,
                nationality_status,
                age,
                daily_rate,
                ot_rate,
                sunday_rate,
                monthly_rate,
                hourly_rate,
                worker_status,
                socso_status
            )
            VALUES
            (
                @id,
                @name,
                @passport,
                @designation_status,
                @status,
                @recommendation,
                @epf_status,
                @nationality_status,
                @age,
                @daily_rate,
                @ot_rate,
                @sunday_rate,
                @monthly_rate,
                @hourly_rate,
                @worker_status,
                @socso_status
            )
            ON CONFLICT (id) DO UPDATE SET
                name               = EXCLUDED.name,
                passport           = EXCLUDED.passport,
                designation_status = EXCLUDED.designation_status,
                status             = EXCLUDED.status,
                recommendation     = EXCLUDED.recommendation,
                epf_status         = EXCLUDED.epf_status,
                nationality_status = EXCLUDED.nationality_status,
                age                = EXCLUDED.age,
                daily_rate         = EXCLUDED.daily_rate,
                ot_rate            = EXCLUDED.ot_rate,
                sunday_rate        = EXCLUDED.sunday_rate,
                monthly_rate       = EXCLUDED.monthly_rate,
                hourly_rate        = EXCLUDED.hourly_rate,
                worker_status      = EXCLUDED.worker_status,
                socso_status       = EXCLUDED.socso_status;
        ";

                    await using var cmd = new NpgsqlCommand(sql, conn, transaction);

                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@name", worker.Name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@passport", worker.Passport ?? string.Empty);
                    cmd.Parameters.AddWithValue("@designation_status", worker.Designation ?? string.Empty);
                    cmd.Parameters.AddWithValue("@status", worker.Status ?? string.Empty);
                    cmd.Parameters.AddWithValue("@recommendation", worker.Recommendation ?? string.Empty);

                    cmd.Parameters.AddWithValue("@epf_status", worker.EPFStatus.ToString());
                    cmd.Parameters.AddWithValue("@nationality_status", worker.NationalityStatus.ToString());
                    cmd.Parameters.AddWithValue("@age", worker.Age);

                    cmd.Parameters.AddWithValue("@daily_rate", worker.DailyRate);
                    cmd.Parameters.AddWithValue("@ot_rate", worker.OTRate);
                    cmd.Parameters.AddWithValue("@sunday_rate", worker.SundayRate);
                    cmd.Parameters.AddWithValue("@monthly_rate", worker.MonthlyRate);
                    cmd.Parameters.AddWithValue("@hourly_rate", worker.HourlyRate);

                    cmd.Parameters.AddWithValue("@worker_status", worker.WorkerStatus.ToString());

                    cmd.Parameters.AddWithValue("@socso_status", worker.SocsoCategory.ToString());

                    //int affected = await cmd.ExecuteNonQueryAsync();
                   // return affected > 0;
                    await cmd.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Worker Submit Error: " + ex);
                return false;
            }
       }


        public async Task<List<WorkerRecord.WorkerRecord>> GetWorkersByStatus(EWorkerStatus status)
        {
            var results = new List<WorkerRecord.WorkerRecord>();

            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                const string sql = @"
            SELECT
                id,
                name,
                passport,
                designation_status,
                status,
                recommendation,
                epf_status,
                nationality_status,
                age,
                daily_rate,
                ot_rate,
                sunday_rate,
                monthly_rate,
                hourly_rate,
                worker_status,
                socso_status
            FROM hr.workerinfo
            WHERE worker_status = @status;
        ";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@status", status.ToString());

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string ParseString(string column) =>
                        reader[column] is DBNull ? string.Empty : reader[column]!.ToString()!;

                    decimal ParseDecimal(string column) =>
                        reader[column] is DBNull ? 0m : reader.GetDecimal(reader.GetOrdinal(column));

                    TEnum ParseEnum<TEnum>(string column, TEnum fallback)
                        where TEnum : struct, Enum
                    {
                        var raw = ParseString(column);
                        return Enum.TryParse<TEnum>(raw, out var value) ? value : fallback;
                    }

                    // 🔒 Prevent side-effects during load
                    var worker = new WorkerRecord.WorkerRecord
                    {
                        IsLoading = true
                    };

                    worker.ID = ParseString("id");
                    worker.Name = ParseString("name");
                    worker.Passport = ParseString("passport");
                    worker.Designation = ParseString("designation_status");
                    worker.Status = ParseString("status");
                    worker.Recommendation = ParseString("recommendation");

                    worker.EPFStatus = ParseEnum("epf_status", EEPFCategory.A);
                    worker.NationalityStatus = ParseEnum("nationality_status", ENationalityStatus.Local);
                    worker.Age = ParseDecimal("age");

                    worker.DailyRate = ParseDecimal("daily_rate");
                    worker.OTRate = ParseDecimal("ot_rate");
                    worker.SundayRate = ParseDecimal("sunday_rate");
                    worker.MonthlyRate = ParseDecimal("monthly_rate");
                    worker.HourlyRate = ParseDecimal("hourly_rate");
                    worker.SocsoCategory = ParseEnum("socso_status", ESocsoCategory.Act4);
                    worker.WorkerStatus = ParseEnum("worker_status", EWorkerStatus.Inactive);

                    // 🔓 Re-enable logic after hydration
                    worker.IsLoading = false;

                    results.Add(worker);
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetWorkersByStatus Error: " + ex);
                return new List<WorkerRecord.WorkerRecord>();
            }
        }

        public void InsertWageRecord(int year, int month, WageRecord wageRecord)
        {
            if (wageRecord?.WageRecords == null || wageRecord.WageRecords.Count == 0)
                return;

            using var conn = new NpgsqlConnection(GetConnectionString());
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Delete existing records
                using (var deleteCmd = new NpgsqlCommand(
                    "DELETE FROM hr.wagesinfo WHERE year = @year AND month = @month", conn))
                {
                    deleteCmd.Parameters.AddWithValue("year", year);
                    deleteCmd.Parameters.AddWithValue("month", month);
                    deleteCmd.ExecuteNonQuery();
                }

                // 2. Insert new records
                foreach (var record in wageRecord.WageRecords)
                {
                    using var insertCmd = new NpgsqlCommand(@"
            INSERT INTO hr.wagesinfo (
                year, month,
                workerid, workername, epf_status,

                daily_hours, ot_hours, sunday_hours, monthly_hours, hourly_hours,
                daily_rate, ot_rate, sunday_rate, monthly_rate, hourly_rate,

                daily_wages, ot_wages, sunday_wages, monthly_wages, hourly_wages,
                socso_status,socso_employee,socso_employer,
                gross_wages,
                epf_employer,
                epf_employee,
                deduction,
                deduction_reason,
                allowance,
                approvedby,
                total_wages
            ) VALUES (
                @year, @month,
                @workerid, @workername, @epf_status,

                @daily_hours, @ot_hours, @sunday_hours, @monthly_hours, @hourly_hours,
                @daily_rate, @ot_rate, @sunday_rate, @monthly_rate, @hourly_rate,

                @daily_wages, @ot_wages, @sunday_wages, @monthly_wages, @hourly_wages,
                @socso_status,@socso_employee,@socso_employer,
                @gross_wages,
                @epf_employer,
                @epf_employee,
                @Deduction,
                @DeductionReason,
                @Allowance,
                @Approvedby,
                @total_wages
            )", conn);

                    // --- Meta ---
                    insertCmd.Parameters.AddWithValue("year", year);
                    insertCmd.Parameters.AddWithValue("month", month);
                    insertCmd.Parameters.AddWithValue("workerid", record.ID ?? string.Empty);
                    insertCmd.Parameters.AddWithValue("workername", record.Name ?? string.Empty);
                    insertCmd.Parameters.AddWithValue("epf_status", record.EPFCategory.ToString());

                    // --- Hours ---
                    insertCmd.Parameters.AddWithValue("daily_hours", record.DailyHours);
                    insertCmd.Parameters.AddWithValue("ot_hours", record.OTHours);
                    insertCmd.Parameters.AddWithValue("sunday_hours", record.SundayHours);
                    insertCmd.Parameters.AddWithValue("monthly_hours", record.MonthlyHours);
                    insertCmd.Parameters.AddWithValue("hourly_hours", record.HourlyHours);

                    // --- Rates ---
                    insertCmd.Parameters.AddWithValue("daily_rate", record.DailyRate);
                    insertCmd.Parameters.AddWithValue("ot_rate", record.OTRate);
                    insertCmd.Parameters.AddWithValue("sunday_rate", record.SundayRate);
                    insertCmd.Parameters.AddWithValue("monthly_rate", record.MonthlyRate);
                    insertCmd.Parameters.AddWithValue("hourly_rate", record.HourlyRate);

                    // --- Wages ---
                    insertCmd.Parameters.AddWithValue("daily_wages", record.Daily_wages);
                    insertCmd.Parameters.AddWithValue("ot_wages", record.OT_wages);
                    insertCmd.Parameters.AddWithValue("sunday_wages", record.Sunday_wages);
                    insertCmd.Parameters.AddWithValue("monthly_wages", record.Monthly_wages);
                    insertCmd.Parameters.AddWithValue("hourly_wages", record.Hourly_wages);

                    // --- Totals / EPF ---
                    insertCmd.Parameters.AddWithValue("gross_wages", record.Gross_wages);
                    insertCmd.Parameters.AddWithValue("epf_employer", record.EPF_Employer);
                    insertCmd.Parameters.AddWithValue("epf_employee", record.EPF_Employee);
                    insertCmd.Parameters.AddWithValue("total_wages", record.Total_wages);
                    insertCmd.Parameters.AddWithValue("socso_status", record.SocsoCategory.ToString());
                    insertCmd.Parameters.AddWithValue("socso_employee", record.Socso_Employee);
                    insertCmd.Parameters.AddWithValue("socso_employer", record.Socso_Employer);
                    insertCmd.Parameters.AddWithValue("@Deduction", record.Deduction);
                    insertCmd.Parameters.AddWithValue("@DeductionReason",
                    string.IsNullOrWhiteSpace(record.Deduction_Reason) ? (object)DBNull.Value : record.Deduction_Reason);
                    insertCmd.Parameters.AddWithValue("@Allowance", record.Allowance);
                    insertCmd.Parameters.AddWithValue("@Approvedby",record.Approvedby);
                    

                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<WageRecord> GetWageRecordAsync(int year, int month)
        {
            var result = new WageRecord
            {
                Year = year,
                Month = month,
                WageRecords = new List<SingleWageRecord>()
            };

            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(@"
            SELECT
                workerid,
                workername,
                epf_status,
             
                daily_hours, ot_hours, sunday_hours, monthly_hours, hourly_hours,
                daily_rate, ot_rate, sunday_rate, monthly_rate, hourly_rate,

                daily_wages, ot_wages, sunday_wages, monthly_wages, hourly_wages,
                socso_status, socso_employee, socso_employer,
                gross_wages,
                epf_employer,
                epf_employee,
                deduction,
                deduction_reason,
                allowance,
                approvedby,
                total_wages
            FROM hr.wagesinfo
            WHERE year = @year AND month = @month
            ORDER BY workername ASC;
        ", conn);

                cmd.Parameters.AddWithValue("year", year);
                cmd.Parameters.AddWithValue("month", month);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var record = new SingleWageRecord
                    {
                        IsLoading = true
                    };

                    // ---------- Strings ----------
                    record.ID = reader.IsDBNull(reader.GetOrdinal("workerid"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("workerid"));

                    record.Name = reader.IsDBNull(reader.GetOrdinal("workername"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("workername"));

                    record.Approvedby = reader.IsDBNull(reader.GetOrdinal("approvedby"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("approvedby"));

                    // ---------- EPF Category ----------
                    var epfCatStr = reader.IsDBNull(reader.GetOrdinal("epf_status"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("epf_status"));

                    record.EPFCategory = Enum.TryParse<EEPFCategory>(epfCatStr, out var epfCat)
                        ? epfCat
                        : default;

                    // ---------- Hours ----------
                    record.DailyHours = reader.IsDBNull(reader.GetOrdinal("daily_hours")) ? 0m : reader.GetDecimal(reader.GetOrdinal("daily_hours"));
                    record.OTHours = reader.IsDBNull(reader.GetOrdinal("ot_hours")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ot_hours"));
                    record.SundayHours = reader.IsDBNull(reader.GetOrdinal("sunday_hours")) ? 0m : reader.GetDecimal(reader.GetOrdinal("sunday_hours"));
                    record.MonthlyHours = reader.IsDBNull(reader.GetOrdinal("monthly_hours")) ? 0m : reader.GetDecimal(reader.GetOrdinal("monthly_hours"));
                    record.HourlyHours = reader.IsDBNull(reader.GetOrdinal("hourly_hours")) ? 0m : reader.GetDecimal(reader.GetOrdinal("hourly_hours"));

                    // ---------- Rates ----------
                    record.DailyRate = reader.IsDBNull(reader.GetOrdinal("daily_rate")) ? 0m : reader.GetDecimal(reader.GetOrdinal("daily_rate"));
                    record.OTRate = reader.IsDBNull(reader.GetOrdinal("ot_rate")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ot_rate"));
                    record.SundayRate = reader.IsDBNull(reader.GetOrdinal("sunday_rate")) ? 0m : reader.GetDecimal(reader.GetOrdinal("sunday_rate"));
                    record.MonthlyRate = reader.IsDBNull(reader.GetOrdinal("monthly_rate")) ? 0m : reader.GetDecimal(reader.GetOrdinal("monthly_rate"));
                    record.HourlyRate = reader.IsDBNull(reader.GetOrdinal("hourly_rate")) ? 0m : reader.GetDecimal(reader.GetOrdinal("hourly_rate"));

                    // ---------- Wages ----------
                    record.Daily_wages = reader.IsDBNull(reader.GetOrdinal("daily_wages")) ? 0m : reader.GetDecimal(reader.GetOrdinal("daily_wages"));
                    record.OT_wages = reader.IsDBNull(reader.GetOrdinal("ot_wages")) ? 0m : reader.GetDecimal(reader.GetOrdinal("ot_wages"));
                    record.Sunday_wages = reader.IsDBNull(reader.GetOrdinal("sunday_wages")) ? 0m : reader.GetDecimal(reader.GetOrdinal("sunday_wages"));
                    record.Monthly_wages = reader.IsDBNull(reader.GetOrdinal("monthly_wages")) ? 0m : reader.GetDecimal(reader.GetOrdinal("monthly_wages"));
                    record.Hourly_wages = reader.IsDBNull(reader.GetOrdinal("hourly_wages")) ? 0m : reader.GetDecimal(reader.GetOrdinal("hourly_wages"));

                    // ---------- Totals ----------
                    record.Gross_wages = reader.IsDBNull(reader.GetOrdinal("gross_wages")) ? 0m : reader.GetDecimal(reader.GetOrdinal("gross_wages"));
                    record.EPF_Employer = reader.IsDBNull(reader.GetOrdinal("epf_employer")) ? 0m : reader.GetDecimal(reader.GetOrdinal("epf_employer"));
                    record.EPF_Employee = reader.IsDBNull(reader.GetOrdinal("epf_employee")) ? 0m : reader.GetDecimal(reader.GetOrdinal("epf_employee"));
                    record.Total_wages = reader.IsDBNull(reader.GetOrdinal("total_wages")) ? 0m : reader.GetDecimal(reader.GetOrdinal("total_wages"));
                    record.Socso_Employee = reader.IsDBNull(reader.GetOrdinal("socso_employee")) ? 0m : reader.GetDecimal(reader.GetOrdinal("socso_employee"));
                    record.Socso_Employer = reader.IsDBNull(reader.GetOrdinal("socso_employer")) ? 0m : reader.GetDecimal(reader.GetOrdinal("socso_employer"));
                    record.Deduction = reader.IsDBNull(reader.GetOrdinal("deduction")) ? 0m : reader.GetDecimal(reader.GetOrdinal("deduction"));
                    record.Deduction_Reason = reader.IsDBNull(reader.GetOrdinal("deduction_reason")) ? string.Empty : reader.GetString(reader.GetOrdinal("deduction_reason"));
                    record.Allowance = reader.IsDBNull(reader.GetOrdinal("allowance")) ? 0m : reader.GetDecimal(reader.GetOrdinal("allowance"));
                   // record.Passport = reader.IsDBNull(reader.GetOrdinal("passport")) ? string.Empty : reader.GetString(reader.GetOrdinal("passport"));
                   // record.Designation = reader.IsDBNull(reader.GetOrdinal("designation_status")) ? string.Empty : reader.GetString(reader.GetOrdinal("designation_status"));

                    // ---------- SOCSO Category ----------
                    var socsoCatStr = reader.IsDBNull(reader.GetOrdinal("socso_status"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("socso_status"));

                    record.SocsoCategory = Enum.TryParse<ESocsoCategory>(socsoCatStr, out var socsoCat)
                        ? socsoCat
                        : default;

                    record.IsLoading = false;

                    result.AddRecord(record);
                }

                return result;
            }
            catch (PostgresException pgEx)
            {
                // 🔹 Database-specific error
                throw new Exception(
                    $"Database error while retrieving wage records for {month}/{year}: {pgEx.Message}",
                    pgEx);
            }
            catch (Exception ex)
            {
                // 🔹 General error
                throw new Exception(
                    $"Unexpected error while retrieving wage records for {month}/{year}: {ex.Message}",
                    ex);
            }
        }
    }

    }

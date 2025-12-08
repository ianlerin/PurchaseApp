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
            Connection = new NpgsqlConnection($"Server={StaticResources.ConnectionId};Port=5432; User Id=postgres; Password=password; Database=Genesis_HR");

        }
        private string GetConnectionString()
        {
            return $@"
            Server={StaticResources.ConnectionId};
            Port=5432;
            User Id=postgres;
            Password=password;
            Database=Genesis_HR
        ";
        }
        public async Task<bool> Submit(WorkerRecord.WorkerRecord info)
        {
            try
            {
                string ID = info.ID;

                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();
                if (string.IsNullOrEmpty(ID))
                {
                    await using var seqCmd = new NpgsqlCommand("SELECT nextval('worker_info_id')", conn);
                    var nextVal = await seqCmd.ExecuteScalarAsync();
                    ID = $"Worker{nextVal}";
                }

                string sql = @"
            INSERT INTO workerinfo
            (id, name, passport, daily_rate, ot_rate, sunday_rate, monthly_rate, worker_status)
            VALUES
            (@id, @name, @passport, @daily_rate, @ot_rate, @sunday_rate, @monthly_rate, @worker_status)
            ON CONFLICT(id) DO UPDATE SET
                name = EXCLUDED.name,
                passport = EXCLUDED.passport,
                daily_rate = EXCLUDED.daily_rate,
                ot_rate = EXCLUDED.ot_rate,
                sunday_rate = EXCLUDED.sunday_rate,
                monthly_rate = EXCLUDED.monthly_rate,
                worker_status = EXCLUDED.worker_status;
        ";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@id", ID ?? "");
                cmd.Parameters.AddWithValue("@name", info.Name ?? "");
                cmd.Parameters.AddWithValue("@passport", info.Passport ?? "");
                cmd.Parameters.AddWithValue("@daily_rate", info.DailyRate);
                cmd.Parameters.AddWithValue("@ot_rate", info.OTRate);
                cmd.Parameters.AddWithValue("@sunday_rate", info.SundayRate);
                cmd.Parameters.AddWithValue("@monthly_rate", info.MonthlyRate);
                cmd.Parameters.AddWithValue("@worker_status", info.WorkerStatus.ToString());

                int affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Worker Submit Error: " + ex);
                return false;
            }
        }



        public async Task<List<WorkerRecord.WorkerRecord>> GetWorkersByStatus(EWorkerStatus status)
        {
            List<WorkerRecord.WorkerRecord> results = new();

            try
            {
                await using var conn = new NpgsqlConnection(GetConnectionString());
                await conn.OpenAsync();

                string sql = @"
            SELECT id, name, passport, daily_rate, ot_rate, sunday_rate, monthly_rate, worker_status
            FROM workerinfo
            WHERE worker_status = @status;
        ";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@status", status.ToString());

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var rawStatus = reader["worker_status"].ToString() ?? "";

                    var worker = new WorkerRecord.WorkerRecord
                    {
                        ID = reader["id"].ToString(),
                        Name = reader["name"].ToString(),
                        Passport = reader["passport"].ToString(),
                        DailyRate = reader.GetDecimal(reader.GetOrdinal("daily_rate")),
                        OTRate = reader.GetDecimal(reader.GetOrdinal("ot_rate")),
                        SundayRate = reader.GetDecimal(reader.GetOrdinal("sunday_rate")),
                        MonthlyRate = reader.GetDecimal(reader.GetOrdinal("monthly_rate")),

                        // Convert string → enum safely
                        WorkerStatus = Enum.TryParse<EWorkerStatus>(rawStatus, out var parsedStatus)
                            ? parsedStatus
                            : EWorkerStatus.Inactive
                    };

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
                // 1. Delete existing records for the year and month
                using (var deleteCmd = new NpgsqlCommand(
                    "DELETE FROM wagesinfo WHERE year = @year AND month = @month", conn))
                {
                    deleteCmd.Parameters.AddWithValue("year", year);
                    deleteCmd.Parameters.AddWithValue("month", month);
                    deleteCmd.ExecuteNonQuery();
                }

                // 2. Insert new wage records
                foreach (var record in wageRecord.WageRecords)
                {
                    using var insertCmd = new NpgsqlCommand(@"
                    INSERT INTO wagesinfo (
                        year, month, workerid, workername,
                        daily_hours, ot_hours, sunday_hours, monthly_hours, hourly_hours,
                        daily_rate, ot_rate, sunday_rate, monthly_rate, hourly_rate,
                        daily_wages, ot_wages, sunday_wages, monthly_wages, hourly_wages, total_wages
                    ) VALUES (
                        @year, @month, @workerid, @workername,
                        @daily_hours, @ot_hours, @sunday_hours, @monthly_hours, @hourly_hours,
                        @daily_rate, @ot_rate, @sunday_rate, @monthly_rate, @hourly_rate,
                        @daily_wages, @ot_wages, @sunday_wages, @monthly_wages, @hourly_wages, @total_wages
                    )", conn);

                    insertCmd.Parameters.AddWithValue("year", year);
                    insertCmd.Parameters.AddWithValue("month", month);
                    insertCmd.Parameters.AddWithValue("workerid", record.ID ?? string.Empty);
                    insertCmd.Parameters.AddWithValue("workername", record.Name ?? string.Empty);

                    insertCmd.Parameters.AddWithValue("daily_hours", record.DailyHours);
                    insertCmd.Parameters.AddWithValue("ot_hours", record.OTHours);
                    insertCmd.Parameters.AddWithValue("sunday_hours", record.SundayHours);
                    insertCmd.Parameters.AddWithValue("monthly_hours", record.MonthlyHours);
                    insertCmd.Parameters.AddWithValue("hourly_hours", record.HourlyHours);

                    insertCmd.Parameters.AddWithValue("daily_rate", record.DailyRate);
                    insertCmd.Parameters.AddWithValue("ot_rate", record.OTRate);
                    insertCmd.Parameters.AddWithValue("sunday_rate", record.SundayRate);
                    insertCmd.Parameters.AddWithValue("monthly_rate", record.MonthlyRate);
                    insertCmd.Parameters.AddWithValue("hourly_rate", record.HourlyRate);

                    insertCmd.Parameters.AddWithValue("daily_wages", record.Daily_wages);
                    insertCmd.Parameters.AddWithValue("ot_wages", record.OT_wages);
                    insertCmd.Parameters.AddWithValue("sunday_wages", record.Sunday_wages);
                    insertCmd.Parameters.AddWithValue("monthly_wages", record.Monthly_wages);
                    insertCmd.Parameters.AddWithValue("hourly_wages", record.Hourly_wages);
                    insertCmd.Parameters.AddWithValue("total_wages", record.Total_wages);

                    insertCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
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
                workerid, workername,
                daily_hours, ot_hours, sunday_hours, monthly_hours, hourly_hours,
                daily_rate, ot_rate, sunday_rate, monthly_rate, hourly_rate,
                daily_wages, ot_wages, sunday_wages, monthly_wages, hourly_wages, total_wages
            FROM wagesinfo
            WHERE year = @year AND month = @month
            ORDER BY workername ASC;
        ", conn);

                cmd.Parameters.AddWithValue("year", year);
                cmd.Parameters.AddWithValue("month", month);

                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var record = new SingleWageRecord
                        {
                            ID = reader["workerid"]?.ToString() ?? "",
                            Name = reader["workername"]?.ToString() ?? "",

                            DailyHours = reader.GetDecimal(reader.GetOrdinal("daily_hours")),
                            OTHours = reader.GetDecimal(reader.GetOrdinal("ot_hours")),
                            SundayHours = reader.GetDecimal(reader.GetOrdinal("sunday_hours")),
                            MonthlyHours = reader.GetDecimal(reader.GetOrdinal("monthly_hours")),
                            HourlyHours = reader.GetDecimal(reader.GetOrdinal("hourly_hours")),
                            
                            DailyRate = reader.GetDecimal(reader.GetOrdinal("daily_rate")),
                            OTRate = reader.GetDecimal(reader.GetOrdinal("ot_rate")),
                            SundayRate = reader.GetDecimal(reader.GetOrdinal("sunday_rate")),
                            MonthlyRate = reader.GetDecimal(reader.GetOrdinal("monthly_rate")),
                            HourlyRate = reader.GetDecimal(reader.GetOrdinal("hourly_rate")),
                            
                            Daily_wages = reader.GetDecimal(reader.GetOrdinal("daily_wages")),
                            OT_wages = reader.GetDecimal(reader.GetOrdinal("ot_wages")),
                            Sunday_wages = reader.GetDecimal(reader.GetOrdinal("sunday_wages")),
                            Monthly_wages = reader.GetDecimal(reader.GetOrdinal("monthly_wages")),
                            Hourly_wages = reader.GetDecimal(reader.GetOrdinal("hourly_wages")),
                            Total_wages = reader.GetDecimal(reader.GetOrdinal("total_wages"))
                          
                            };

                        result.WageRecords.Add(record);
                    }
                    catch (Exception ex)
                    {
                        // Log the specific record read error
                        Console.WriteLine($"Failed to read a wage record: {ex.Message}");
                        // Optionally continue to next record
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                // Log database-specific errors
                Console.WriteLine($"Database error: {ex.Message}");
                throw; // Re-throw if you want the caller to handle it
            }
            catch (Exception ex)
            {
                // Log any other errors
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw;
            }

            return result;
        }
    }

    }

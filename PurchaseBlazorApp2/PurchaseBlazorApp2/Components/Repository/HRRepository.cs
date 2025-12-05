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
                string ID=info.ID;
               
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
                cmd.Parameters.AddWithValue("@worker_status", (int)info.WorkerStatus);

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

    }
}

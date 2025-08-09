using Npgsql;
using NpgsqlTypes;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections;

namespace PurchaseBlazorApp2.Components
{
    class ApiDb
    {
        private NpgsqlConnection conn;
        public Regex reUuid;

        public ApiDb()
        {
            conn = new NpgsqlConnection($"Server=localhost;Port=5432; User Id=postgres; Password=password; Database=purchase");
            reUuid = new Regex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$");

            return;
        }

        public List<Dictionary<string, object>> executeCmd(string cmdText, List<KeyValuePair<string, List<string>>> listCol = null, Dictionary<string, object> dictParam = null)
        {
            List<Dictionary<string, object>> listRow = new List<Dictionary<string, object>>();
            conn.Open();

            using (NpgsqlCommand cmd = new NpgsqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = cmdText;

                if ((dictParam != null) && (dictParam.Count > 0))
                {
                    foreach (KeyValuePair<string, object> kvp in dictParam)
                    {
                        if ((kvp.Value is IList) && kvp.Value.GetType().IsGenericType)
                        {
                            NpgsqlDbType npgsqlType;

                            if (kvp.Value is List<bool>)
                            {
                                npgsqlType = NpgsqlDbType.Boolean;
                            }
                            else if ((kvp.Value is List<DateTime>) || (kvp.Value is List<DateTime?>))
                            {
                                // npgsqlType = NpgsqlDbType.TimestampTz;
                                npgsqlType = NpgsqlDbType.Timestamp;
                            }
                            else
                            {
                                npgsqlType = NpgsqlDbType.Text;
                            }

                            cmd.Parameters.AddWithValue($"@{kvp.Key}", NpgsqlDbType.Array | npgsqlType, kvp.Value);
                        }
                        else
                        {
                            if (reUuid.IsMatch(kvp.Value.ToString()))
                            {
                                cmd.Parameters.AddWithValue($"@{kvp.Key}", new Guid(kvp.Value.ToString()));
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value);
                            }
                        }
                    }
                }

                if (cmdText.ToUpper().IndexOf("SELECT") == 0)
                {
                    string tableCol;

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listRow.Add(new Dictionary<string, object>());

                            foreach (KeyValuePair<string, List<string>> kvp in listCol)
                            {
                                foreach (string col in kvp.Value)
                                {
                                    tableCol = col != "*" ? $"{kvp.Key}__{col}" : $"{kvp.Key}__asterisk";
                                    listRow.Last().Add(tableCol, reader[tableCol]);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (cmd.ExecuteNonQuery() <= 0)
                    {
                        Trace.WriteLine($"Error in InvoiceRepository.executeCmd ExecuteNonQuery {cmd.CommandText}");
                    }
                }
            }

            conn.Close();

            return listRow;
        }

        public List<Dictionary<string, object>> select(List<KeyValuePair<string, List<string>>> listCol, string operation = null, string condition = null, Dictionary<string, object> dictParam = null)
        {
            string cmdText = "select ";

            foreach (KeyValuePair<string, List<string>> kvp in listCol)
            {
                foreach (string col in kvp.Value)
                {
                    if (col != "*")
                    {
                        cmdText += $"{operation}({kvp.Key}.{col}) as {kvp.Key}__{col}, ";
                    }
                    else
                    {
                        cmdText += $"{operation}(*) as {kvp.Key}__asterisk, ";
                    }
                }
            }

            cmdText = cmdText.TrimEnd(' ').TrimEnd(',');
            cmdText += $" from {listCol[0].Key} {condition};";
            /*
            Trace.WriteLine("debug0");
            Trace.WriteLine(cmdText);
            Trace.WriteLine("");
            */
            List<Dictionary<string, object>> listRow = executeCmd(cmdText, listCol, dictParam);

            return listRow;
        }

        public long getCount(string table, string cmdTextCondition, Dictionary<string, object> dictParam = null)
        {
            long cnt;

            cnt = (long)select(
                new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>(
                        table,
                        new List<string> { "*" })
                },
                "count",
                cmdTextCondition,
                dictParam)[0][$"{table}__asterisk"];

            return cnt;
        }

        public Type getType(object dbType)
        {
            Type nativeType = null;

            switch ((uint)dbType)
            {
                case 1114:
                case 1184:
                    nativeType = typeof(DateTime?);
                    break;
                case 1082:
                    nativeType = typeof(DateOnly?);
                    break;
                case 1700:
                    nativeType = typeof(decimal);
                    break;
                case 701:
                    nativeType = typeof(double);
                    break;
                case 20:
                case 21:
                case 23:
                    nativeType = typeof(long);
                    break;
                case 25:
                case 869:
                case 1043:
                case 2950:
                    nativeType = typeof(string);
                    break;
                case 1083:
                    nativeType = typeof(TimeSpan?);
                    break;
            }

            return nativeType;
        }

        public List<KeyValuePair<string, List<Type>>> selectType(List<KeyValuePair<string, List<string>>> listCol, string condition = null)
        {
            string cmdText0 = "select ";
            string cmdText1 = " from (select ";
            string cmdText2 = "union select ";

            foreach (KeyValuePair<string, List<string>> kvp in listCol)
            {
                foreach (string col in kvp.Value)
                {
                    cmdText0 += $"pg_typeof({kvp.Key}__{col}) as {kvp.Key}__{col}, ";
                    cmdText1 += $"{kvp.Key}.{col} as {kvp.Key}__{col}, ";
                    cmdText2 += "null, ";
                }
            }

            cmdText0 = cmdText0.TrimEnd(' ').TrimEnd(',');
            cmdText1 = cmdText1.TrimEnd(' ').TrimEnd(',');
            cmdText2 = cmdText2.TrimEnd(' ').TrimEnd(',');

            // cmdText1 += $" from {string.Join(", ", listCol.Select(i => i.Key).ToList())} ";
            cmdText1 += $" from {listCol[0].Key} {condition} ";
            Dictionary<string, object> dictDbType = executeCmd($"{cmdText0}{cmdText1}{cmdText2} limit 1);", listCol)[0];

            List<KeyValuePair<string, List<Type>>> listType = new List<KeyValuePair<string, List<Type>>>();

            foreach (KeyValuePair<string, List<string>> kvp in listCol)
            {
                listType.Add(new KeyValuePair<string, List<Type>>(kvp.Key, new List<Type>()));

                foreach (string col in kvp.Value)
                {
                    listType.Last().Value.Add(getType(dictDbType[$"{kvp.Key}__{col}"]));
                }
            }

            return listType;
        }

        public void insert(string nameTable, Dictionary<string, object> dictParam)
        {
            string cmdText0 = $"insert into {nameTable} (";
            string cmdText1 = "select * from unnest(";

            foreach (string nameCol in dictParam.Keys)
            {
                cmdText0 += $"{nameCol}, ";
                cmdText1 += $"@{nameCol}, ";
            }

            cmdText0 = cmdText0.TrimEnd().TrimEnd(',');
            cmdText1 = cmdText1.TrimEnd().TrimEnd(',');

            executeCmd($"{cmdText0}) {cmdText1});", dictParam: dictParam);

            return;
        }

        public void update(string nameTable, Dictionary<string, object> dictParam, string condition = null, string tableTmp = "tableTmp")
        {
            string cmdText0 = $"update {nameTable} set ";
            string cmdText1 = " from (select ";

            foreach (KeyValuePair<string, object> kvp in dictParam)
            {
                if (!(condition != null && condition.Contains(kvp.Key)))
                {
                    cmdText0 += $"{kvp.Key} = {tableTmp}.{kvp.Key},";
                    cmdText1 += $"unnest(@{kvp.Key}) as {kvp.Key},";
                }
            }

            cmdText0 = cmdText0.TrimEnd(',');
            cmdText1 = cmdText1.TrimEnd(',');
            cmdText1 += $") as {tableTmp} {condition};";

            executeCmd(cmdText0 + cmdText1, dictParam: dictParam);

            return;
        }

        public void delete(string nameTable, Dictionary<string, object> dictParam = null, string condition = null)
        {
            string cmdText = $"delete from {nameTable} {condition};";
            executeCmd(cmdText, dictParam: dictParam);

            return;
        }

        public void createTable(string nameTable, Dictionary<string, string> dictColType)
        {
            string cmdText = $"create table if not exists {nameTable} (" +
                $"pk uuid primary key default gen_random_uuid(), ";

            foreach (KeyValuePair<string, string> kvp in dictColType)
            {
                cmdText += $"{kvp.Key} {kvp.Value}, ";
            }

            cmdText = cmdText.TrimEnd().TrimEnd(',');
            cmdText += ");";

            executeCmd(cmdText);

            return;
        }

        public void createFunc(string funcText)
        {
            string cmdText = "do $main$ begin ";
            cmdText += funcText;
            cmdText += "exception when duplicate_function then null; " +
                "end; $main$;";

            executeCmd(cmdText);

            return;
        }

        public async Task createTrigger(string triggerText, string triggerFunc)
        {
            string cmdText = "do $main$ begin ";
            cmdText += triggerText;
            cmdText += $"for each row execute function {triggerFunc}();" +
                $"exception when duplicate_object then null;" +
                $"end; $main$;";

            executeCmd(cmdText);

            return;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using PurchaseBlazorApp2.Components;
using SharedDataType;
using System.Collections.ObjectModel;
using System.Reflection;

namespace PurchaseBlazorApp2.Controller
{
    [Route("api/table")]
    [ApiController]
    public class TableController : ControllerBase
    {
        public List<KeyValuePair<string, List<string>>> listCol;
        public TableConfig tableConfig;
        private string primaryKeyCol = null;
        private List<KeyValuePair<string, List<Type>>> listType;
        private List<KeyValuePair<string, List<string>>> listColTotal;
        private bool bTotal = false;
        private List<string> listColFlat;

        private ApiDb apiDb;
        private List<Dictionary<string, object>> listDbRow = null;
        private Dictionary<string, object> dictTotal = null;

        private object dynamicTypeObj = null;
        public string cmdTextOrder = null;
        private string cmdTextCondition = null;
        private Dictionary<string, object> dictParam;

        private int _totalRow = 0;
        private int _totalPage = 0;

        public Dictionary<string, object> dictFilter;

        public void addPrimaryKeyCol()
        {
            primaryKeyCol = apiDb.select(
                new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>(
                        "pg_attribute",
                        new List<string> { "attname" })
                },
                condition: "join pg_index i on " +
                "pg_attribute.attrelid = i.indrelid and " +
                "pg_attribute.attnum = any(i.indkey) " +
                "where i.indrelid = @table::regclass and " +
                "i.indisprimary;",
                dictParam: new Dictionary<string, object>
                {
                    { "table", listCol[0].Key }
                })[0]["pg_attribute__attname"].ToString();

            if (!listCol[0].Value.Contains(primaryKeyCol))
            {
                listCol[0].Value.Insert(0, primaryKeyCol);
            }

            return;
        }

        public void initFilter()
        {
            dictFilter = new Dictionary<string, object>();

            for (int i = 0; i < listCol.Count; i++)
            {
                for (int j = 0; j < listCol[i].Value.Count; j++)
                {
                    string colName = $"{listCol[i].Key}__{listCol[i].Value[j]}";
                    object objPropFilterPredefined = null;

                    if (tableConfig.dictFilterPredefined != null &&
                        tableConfig.dictFilterPredefined.ContainsKey(listCol[i].Key) &&
                        tableConfig.dictFilterPredefined[listCol[i].Key].ContainsKey(listCol[i].Value[j]))
                    {
                        objPropFilterPredefined = tableConfig.dictFilterPredefined[listCol[i].Key][listCol[i].Value[j]];
                    }

                    if ((listType[i].Value[j] == typeof(DateTime?)) ||
                        (listType[i].Value[j] == typeof(DateOnly?)))
                    {
                        dictFilter.Add(colName, new DateFilter(colName, objPropFilterPredefined != null ? (List<DateTime?>)objPropFilterPredefined : null));
                    }
                    else if ((listType[i].Value[j] == typeof(decimal)) ||
                        (listType[i].Value[j] == typeof(double)) ||
                        (listType[i].Value[j] == typeof(int)))
                    {
                        dictFilter.Add(colName, new NumberFilter(colName));
                    }
                    else if (listType[i].Value[j] == typeof(TimeSpan?))
                    {
                        dictFilter.Add(colName, new TimeFilter(colName));
                    }
                    else
                    {
                        dictFilter.Add(colName, new TextFilter(colName, objPropFilterPredefined != null ? (List<string>)objPropFilterPredefined : null));
                    }
                }
            }

            return;
        }

        /*
        [HttpGet("toFirstPage")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> toFirstPage()
        {
            firstRowIndex = 0;
            updatePageDisplay();

            return Ok(listDbRow);
        }

        [HttpGet("toPreviousPage")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> toPreviousPage()
        {
            firstRowIndex -= tableConfig.rowPerPage;
            updatePageDisplay();

            return Ok(listDbRow);
        }

        [HttpGet("toNextPage")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> toNextPage()
        {
            firstRowIndex += tableConfig.rowPerPage;
            updatePageDisplay();

            return Ok(listDbRow);
        }

        [HttpGet("toNextPage")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> toLastPage()
        {
            _totalRow = (int)apiDb.getCount(listCol[0].Key, cmdTextCondition, dictParam);
            firstRowIndex = (_totalRow % tableConfig.rowPerPage) == 0 ? _totalRow - tableConfig.rowPerPage : Convert.ToInt32(Math.Floor(Convert.ToDouble(_totalRow) / tableConfig.rowPerPage) * tableConfig.rowPerPage);
            updatePageDisplay();

            return Ok(listDbRow);
        }

        [HttpPost("toSelectedPage")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> toSelectedPage([FromBody] int _firstRowIndex)
        {
            firstRowIndex = _firstRowIndex;
            updatePageDisplay();

            return Ok(listDbRow);
        }
        */
        [HttpPost("searchFilter")]
        public ActionResult<ContentTable> searchFilter([FromBody] ParamSearch paramSearch)
        {
            List<string> listCmdText = new List<string>();
            object cmdTextObj;
            dictParam.Clear();
            MethodInfo methodInfo;
            Type filterType;

            // Read filter parameters
            if (paramSearch.dictFilter != null)
            {
                foreach (KeyValuePair<string, object> kvp in paramSearch.dictFilter)
                {
                    filterType = dictFilter[kvp.Key].GetType();

                    foreach (PropertyInfo propInfo in dictFilter[kvp.Key].GetType().GetProperties())
                    {
                        filterType.GetProperty(propInfo.Name).SetValue(dictFilter[kvp.Key], propInfo.GetValue(kvp.Value));
                    }
                }
            }

            // Construct SQL command with filter parameters
            foreach (KeyValuePair<string, object> kvp in dictFilter)
            {
                filterType = kvp.Value.GetType();
                methodInfo = filterType.GetMethod(nameof(DisplayFilter.getCmdText));
                cmdTextObj = methodInfo.Invoke(kvp.Value, null);

                if (cmdTextObj != null)
                {
                    listCmdText.Add(cmdTextObj.ToString());
                    methodInfo = filterType.GetMethod(nameof(DisplayFilter.getDictParam));
                    dictParam = dictParam.Concat((Dictionary<string, object>)methodInfo.Invoke(kvp.Value, null)).ToDictionary();
                }

                methodInfo = filterType.GetMethod(nameof(DisplayFilter.setDesc));
                methodInfo.Invoke(kvp.Value, null);
            }

            // Construct SQL join command
            cmdTextCondition = tableConfig.cmdTextJoin;
            cmdTextCondition = listCmdText.Count > 0 ? $"{cmdTextCondition} where {string.Join(" and ", listCmdText)}" : cmdTextCondition;

            // Construct SQL order by command
            if (listCol.Select(i => i.Key).Contains(paramSearch.tableOrder.table) &&
                listCol.Where(i => i.Key == paramSearch.tableOrder.table)
                .Any(j => j.Value.Contains(paramSearch.tableOrder.col)))
            {
                cmdTextOrder = paramSearch.tableOrder.col == null ? "" : $"order by {paramSearch.tableOrder.table}.{paramSearch.tableOrder.col} {(paramSearch.tableOrder.bAsc ? "asc" : "desc")}";
            }

            // Run SQL command to get selected rows and total row number
            listDbRow = apiDb.select(listCol, condition: $"{cmdTextCondition} {cmdTextOrder} offset {paramSearch.firstRowIndex} limit {tableConfig.rowPerPage}", dictParam: dictParam);

            if (bTotal)
            {
                dictTotal = (apiDb.select(listColTotal, "sum", cmdTextCondition, dictParam))[0];
                listDbRow.Append(dictTotal);
            }

            _totalRow = (int)(apiDb.getCount(listCol[0].Key, cmdTextCondition, dictParam));

            ContentTable contentTable = new ContentTable
            {
                listDbRow = listDbRow,
                listColFlat = listColFlat,
                totalRow = _totalRow,
                rowPerPage = tableConfig.rowPerPage
            };

            return Ok(contentTable);
        }
        /*
        [HttpPost("orderCol")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> orderCol([FromBody] TableOrder tableOrder)
        {
            if(listCol.Select(i => i.Key).Contains(tableOrder.table) &&
                listCol.Where(i => i.Key == tableOrder.table)
                .Any(j => j.Value.Contains(tableOrder.col)))
            {
                cmdTextOrder = $"order by {tableOrder.table}.{tableOrder.col} {(tableOrder.bAsc ? "asc" : "desc")}";
            }

            firstRowIndex = 0;
            updatePageDisplay();

            return Ok(listDbRow);
        }
        
        [HttpPost("resetFilter")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> resetFilter([FromBody] string colName)
        {
            MethodInfo methodInfo = dictFilter[colName].GetType().GetMethod(nameof(DisplayFilter.reset));
            methodInfo.Invoke(dictFilter[colName], null);

            searchFilter(null);
            firstRowIndex = 0;
            updatePageDisplay();

            return Ok(listDbRow);
        }

        [HttpGet("resetAllFilter")]
        public async Task<ActionResult<List<Dictionary<string, object>>>> resetAllFilter()
        {
            MethodInfo methodInfo;

            foreach (object displayFilter in dictFilter.Values)
            {
                methodInfo = displayFilter.GetType().GetMethod(nameof(DisplayFilter.reset));
                methodInfo.Invoke(displayFilter, null);
            }

            searchFilter(null);
            firstRowIndex = 0;
            updatePageDisplay();

            return Ok(listDbRow);
        }
        */
        // public TableController(List<KeyValuePair<string, List<string>>> _listCol, TableConfig _tableConfig = null)
        public TableController()
        {
            // listCol = _listCol;
            listCol = new List<KeyValuePair<string, List<string>>>
            {
                new KeyValuePair<string, List<string>>(
                    "prtable",
                    new List<string>
                    {
                        "requisitionnumber",
                        "requestor",
                        "prstatus",
                        "purpose"
                    })
            };
            // tableConfig = _tableConfig == null ? new TableConfig() : _tableConfig;
            tableConfig = new TableConfig();
            apiDb = new ApiDb();
            addPrimaryKeyCol();
            listType = apiDb.selectType(listCol, tableConfig.cmdTextJoin);
            listColTotal = listCol.Zip(listType, (i, j) => new KeyValuePair<string, List<string>>(
                i.Key,
                i.Value.Zip(j.Value, (k, l) => (k, l))
                .Where(m => (m.l == typeof(decimal) || m.l == typeof(double) || m.l == typeof(int) || m.l == typeof(long)))
                .Select(m => m.k)
                .ToList()))
                .ToList();
            bTotal = listColTotal.Select(i => i.Value.Count)
                .Any(j => j > 0);
            listColFlat = listCol.SelectMany(i => Enumerable.Repeat(i.Key, i.Value.Count))
                .Zip(listCol.SelectMany(i => i.Value), (table, col) => $"{table}__{col}")
                .ToList();
            List<Type> listTypeFlat = listType.SelectMany(i => i.Value)
                .ToList();

            dictParam = new Dictionary<string, object>();
            initFilter();
            
            return;
        }
        
        public async Task<List<string>> getPrimaryKeyFilter()
        {
            List<KeyValuePair<string, List<string>>> listColPrimaryKey = new List<KeyValuePair<string, List<string>>>
            {
                new KeyValuePair<string, List<string>>(
                    listCol[0].Key,
                    new List<string> { primaryKeyCol })
            };

            List<string> listPrimaryKey = apiDb.select(listColPrimaryKey, condition: $"{cmdTextCondition} {cmdTextOrder}", dictParam: dictParam)
                .Select(i => (string)i[$"{listCol[0].Key}__{primaryKeyCol}"])
                .ToList();

            return listPrimaryKey;
        }

        public void searchFilterPredefined(string nameTable, string nameCol, string nameProp, object objProp)
        {
            object objFilter = dictFilter[$"{nameTable}__{nameCol}"];
            PropertyInfo propInfo = objFilter.GetType().GetProperty(nameProp);
            propInfo.SetValue(objFilter, objProp);

            return;
        }

        public Dictionary<string, object> getDictParam()
        {
            return dictParam;
        }
    }

    public class TableConfig
    {
        public int rowPerPage = 20;
        public string cmdTextJoin = null;
        public Dictionary<string, Dictionary<string, object>> dictFilterPredefined = null;
    }

    public abstract class DisplayFilter
    {
        public string colName;
        public string desc = null;

        public abstract Task reset();
        public abstract string getCmdText();
        public abstract Dictionary<string, object> getDictParam();
        public abstract void setDesc();

        public DisplayFilter(string _colName)
        {
            colName = _colName;

            return;
        }

        public DisplayFilter(string tableName, string _colName)
        {
            colName = $"{tableName}__{_colName}";

            return;
        }
    }

    public class TextFilter : DisplayFilter
    {
        private ApiDb apiDb;
        private string[] tableCol = null;
        private List<Dictionary<string, object>> listQueryResult = null;

        public List<string> listItem;
        public string selectedItem = null;
        public string subText = null;
        public List<string> listSelectedItemPredefined = null;

        private void FilterComboBoxItems()
        {
            if (string.IsNullOrEmpty(subText) && listQueryResult != null)
            {
                // Reset to full list when search box is empty
                listItem = listQueryResult
                    .Select(i => i[colName].ToString())
                    .Distinct()
                    .ToList();
            }
            else
            {
                if (listQueryResult != null)
                {
                    // Filter items based on search text (case-insensitive)
                    listItem = listQueryResult
                        .Select(i => i[colName].ToString())
                        .Where(item => item.IndexOf(subText, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Distinct()
                        .ToList();
                }
            }
        }

        public TextFilter(string _colName, List<string> _listSelectedItemPredefined = null) :
            base(_colName)
        {
            listSelectedItemPredefined = _listSelectedItemPredefined;
            apiDb = new ApiDb();
            reset();

            return;
        }

        public TextFilter(string tableName, string _colName, List<string> _listSelectedItemPredefined = null) :
            base(tableName, _colName)
        {
            listSelectedItemPredefined = _listSelectedItemPredefined;
            apiDb = new ApiDb();
            reset();

            return;
        }

        public override async Task reset()
        {
            desc = null;
            selectedItem = null;
            subText = null;
            tableCol = colName.Split("__");

            listQueryResult = apiDb.select(
                new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>(
                        tableCol[0],
                        new List<string>
                        {
                            tableCol[1]
                        })
                },
                condition: $"where {colName.Replace("__", ".")} is not null " +
                (listSelectedItemPredefined != null ? $"and {colName.Replace("__", ".")} = any(@{colName}_pre) " : "") +
                $"order by {colName.Replace("__", ".")}",
                dictParam: listSelectedItemPredefined != null ?
                new Dictionary<string, object>
                {
                    { $"{colName}_pre", listSelectedItemPredefined }
                } : null);
            listItem = listQueryResult
                .Select(i => i[colName].ToString())
                .Distinct()
                .ToList();

            return;
        }

        public override string getCmdText()
        {
            string cmdText = null;

            if (selectedItem != null)
            {
                cmdText = $"{colName.Replace("__", ".")} = @{colName}";
            }
            else if (subText != null)
            {
                cmdText = $"{colName.Replace("__", ".")} ilike @{colName}";
            }

            if (listSelectedItemPredefined != null)
            {
                if (cmdText != null)
                {
                    cmdText += " and ";
                }

                cmdText += $"{colName.Replace("__", ".")} = any(@{colName}_pre)";
            }

            return cmdText;
        }

        public override Dictionary<string, object> getDictParam()
        {
            Dictionary<string, object> dictParam = null;

            if (selectedItem != null)
            {
                dictParam = new Dictionary<string, object>
                {
                    { colName, selectedItem }
                };
            }
            else if (subText != null)
            {
                dictParam = new Dictionary<string, object>
                {
                    { colName, $"%{subText}%" }
                };
            }

            if (listSelectedItemPredefined != null)
            {
                Dictionary<string, object> dictParamPredefined = new Dictionary<string, object>
                {
                    { $"{colName}_pre", listSelectedItemPredefined }
                };

                dictParam = dictParam != null ? dictParam.Concat(dictParamPredefined).ToDictionary() : dictParamPredefined;
            }

            return dictParam;
        }

        public override void setDesc()
        {
            if (selectedItem != null)
            {
                desc = $"Filter {colName}: {selectedItem}";
            }
            else if (subText != null)
            {
                desc = $"Filter {colName}: Texts containing \"{subText}\"";
            }
            else
            {
                desc = null;
            }

            return;
        }

        public void onResetOtherFilter(object param)
        {
            switch (param.ToString())
            {
                case "resetComboBox":
                    selectedItem = null;
                    break;
                case "resetTextBox":
                    subText = null;
                    break;
            }

            return;
        }
    }

    public class DateFilter : DisplayFilter
    {
        public DateTime? startDate = null;
        public DateTime? endDate = null;
        public DateTime? startDatePredefined = null;
        public DateTime? endDatePredefined = null;

        public DateFilter(string _colName, List<DateTime?> listDatePredefined = null) :
            base(_colName)
        {
            if (listDatePredefined != null)
            {
                startDatePredefined = listDatePredefined[0];
                endDatePredefined = listDatePredefined[1];
            }

            reset();

            return;
        }

        public DateFilter(string tableName, string _colName, List<DateTime?> listDatePredefined = null) :
            base(tableName, _colName)
        {
            if (listDatePredefined != null)
            {
                startDatePredefined = listDatePredefined[0];
                endDatePredefined = listDatePredefined[1];
            }

            reset();

            return;
        }

        public override async Task reset()
        {
            startDate = null;
            endDate = null;

            return;
        }

        public override string getCmdText()
        {
            List<string> listCmdText = new List<string>();

            if (startDate.HasValue)
            {
                listCmdText.Add($"{colName.Replace("__", ".")} >= @{colName}_start");
            }

            if (endDate.HasValue)
            {
                listCmdText.Add($"{colName.Replace("__", ".")} <= @{colName}_end");
            }

            if (startDatePredefined.HasValue)
            {
                listCmdText.Add($"{colName.Replace("__", ".")} >= @{colName}_startPre");
            }

            if (endDatePredefined.HasValue)
            {
                listCmdText.Add($"{colName.Replace("__", ".")} <= @{colName}_endPre");
            }

            string cmdText = string.Join(" and ", listCmdText);
            cmdText = cmdText == "" ? null : cmdText;

            return cmdText;
        }

        public override Dictionary<string, object> getDictParam()
        {
            if (startDate.HasValue || endDate.HasValue || startDatePredefined.HasValue || endDatePredefined.HasValue)
            {
                Dictionary<string, object> dictParam = new Dictionary<string, object>();

                if (startDate.HasValue)
                {
                    dictParam.Add($"{colName}_start", startDate);
                }

                if (endDate.HasValue)
                {
                    dictParam.Add($"{colName}_end", endDate);
                }

                if (startDatePredefined.HasValue)
                {
                    dictParam.Add($"{colName}_startPre", startDatePredefined);
                }

                if (endDatePredefined.HasValue)
                {
                    dictParam.Add($"{colName}_endPre", endDatePredefined);
                }

                return dictParam;
            }

            return null;
        }

        public override void setDesc()
        {
            if (startDate.HasValue || endDate.HasValue)
            {
                desc = $"Filter {colName}: ";

                if (startDate.HasValue)
                {
                    desc += $"From {((DateTime)startDate).ToString("yyyy/MM/dd")} ";
                }

                if (endDate.HasValue)
                {
                    desc += $"to {((DateTime)endDate).ToString("yyyy/MM/dd")}";
                }
            }
            else
            {
                desc = null;
            }

            return;
        }
    }

    public class NumberFilter : DisplayFilter
    {
        public double lowerBound = 0;
        public double upperBound = 0;

        public NumberFilter(string _colName) :
            base(_colName)
        {
            reset();

            return;
        }

        public override async Task reset()
        {
            lowerBound = 0;
            upperBound = 0;

            return;
        }

        public override string getCmdText()
        {
            string cmdText = !((lowerBound == 0) && (upperBound == 0)) ? $"{colName.Replace("__", ".")} >= @{colName}_lower and {colName.Replace("__", ".")} <= @{colName}_upper" : null;

            return cmdText;
        }

        public override Dictionary<string, object> getDictParam()
        {
            if (!((lowerBound == 0) && (upperBound == 0)))
            {
                Dictionary<string, object> dictParam = new Dictionary<string, object>
                {
                    { $"{colName}_lower", lowerBound },
                    { $"{colName}_upper", upperBound }
                };

                return dictParam;
            }

            return null;
        }

        public override void setDesc()
        {
            if (!((lowerBound == 0) && (upperBound == 0)))
            {
                desc = $"Filter {colName}: From {lowerBound} to {upperBound}";
            }
            else
            {
                desc = null;
            }

            return;
        }
    }

    public class TimeFilter : DisplayFilter
    {
        private TimeSpan startTime = TimeSpan.Zero;
        private TimeSpan endTime = TimeSpan.Zero;

        public ObservableCollection<int> listHour;
        public ObservableCollection<int> listMins;

        public int selectedStartHour = 0;
        public int selectedStartMins = 0;
        public int selectedEndHour = 0;
        public int selectedEndMins = 0;

        public TimeFilter(string _colName) :
            base(_colName)
        {
            listHour = new ObservableCollection<int>(Enumerable.Range(0, 24));
            listMins = new ObservableCollection<int>(Enumerable
                .Range(0, 12)
                .Select(i => i * 5));
            reset();

            return;
        }

        public override async Task reset()
        {
            startTime = TimeSpan.Zero;
            endTime = TimeSpan.Zero;

            selectedStartHour = 0;
            selectedStartMins = 0;
            selectedEndHour = 0;
            selectedEndMins = 0;

            return;
        }

        public override string getCmdText()
        {
            string cmdText = !((startTime == TimeSpan.Zero) && (endTime == TimeSpan.Zero)) ? $"{colName.Replace("__", ".")} >= @{colName}_start and {colName.Replace("__", ".")} <= @{colName}_end" : null;

            return cmdText;
        }

        public override Dictionary<string, object> getDictParam()
        {
            if (!((startTime == TimeSpan.Zero) && (endTime == TimeSpan.Zero)))
            {
                Dictionary<string, object> dictParam = new Dictionary<string, object>
                {
                    { $"{colName}_start", startTime },
                    { $"{colName}_end", endTime }
                };

                return dictParam;
            }

            return null;
        }

        public override void setDesc()
        {
            if (!((startTime == TimeSpan.Zero) && (endTime == TimeSpan.Zero)))
            {
                desc = $"Filter {colName}: From {startTime} to {endTime}";
            }
            else
            {
                desc = null;
            }

            return;
        }
    }
}
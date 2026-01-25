using ClosedXML.Excel;
using Microsoft.JSInterop;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;

namespace PurchaseBlazorApp2.Client;

public class ExcelWriter
{
    private IJSRuntime JS;
    private XLWorkbook xlWorkbook;
    private IXLWorksheet ixlWorksheet;
    private int row = 0;
    private int col = 0;

    private ExcelWriterConfig config = null;
    private List<Type> listTypeObj;
    private string nameObjNest = null;
    private List<string> listNameObjNest;
    private List<string> listNameField;
    private List<int> listISort;

    private bool bListSystem = false;
    private Dictionary<string, string> dictNameCollectNest;

    private Dictionary<string, object> dictNameObjNest;
    private bool bSet = false;

    public ExcelWriter(IJSRuntime _JS)
    {
        JS = _JS;
        xlWorkbook = new XLWorkbook();
        ixlWorksheet = xlWorkbook.Worksheets.Add(1);

        listTypeObj = new List<Type>();
        listNameObjNest = new List<string>();
        listNameField = new List<string>();

        dictNameCollectNest = new Dictionary<string, string>();
        dictNameObjNest = new Dictionary<string, object>();

        return;
    }

    public int getCount(ICollection collect)
    {
        int count = 0;

        foreach(object obj in collect)
        {
            count++;
        }

        return count;
    }

    public void mergeCell()
    {
        foreach(Tuple<int, int, int, int> tupMerge in config.listMerge)
        {
            ixlWorksheet.Cell(tupMerge.Item1 + 1, tupMerge.Item2 + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ixlWorksheet.Cell(tupMerge.Item1 + 1, tupMerge.Item2 + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ixlWorksheet.Range(tupMerge.Item1 + 1, tupMerge.Item2 + 1, tupMerge.Item3 + 1, tupMerge.Item4 + 1).Merge();
        }

        return;
    }

    public Type getTypeElement(ICollection collect)
    {
        Type type = collect.GetType().GetGenericArguments().Single();

        if(type == typeof(object))
        {
            type = ((Collection<object>)collect).First().GetType();
        }

        return type;
    }

    public void pushNameObjNest(string nameObj)
    {
        nameObjNest += nameObjNest == null ? nameObj : $"___{nameObj}";

        return;
    }

    public void popNameObjNest()
    {
        nameObjNest = string.Join("___", nameObjNest.Split("___")[..^1]);

        return;
    }

    public bool getBInclude(string nameObj, string nameProp)
    {
        bool bInclude = true;

        if (config.dictIgnore.ContainsKey(nameObj) &&
            config.dictIgnore[nameObj].Contains(nameProp))
        {
            bInclude = false;
        }
        else if(config.dictInclude.ContainsKey(nameObj) &&
            !config.dictInclude[nameObj].Contains(nameProp))
        {
            bInclude = false;
        }

        return bInclude;
    }

    public bool getBTypeSystem(Type type)
    {
        bool bTypeSystem = type.FullName.StartsWith("System.") || type.IsEnum;

        return bTypeSystem;
    }

    public bool getBIncludeRow(string nameObj, object obj)
    {
        bool bIncludeRow = !(config.dictPropBInclude.ContainsKey(nameObj) &&
            !(bool)obj.GetType().GetProperty(config.dictPropBInclude[nameObj]).GetValue(obj));

        return bIncludeRow;
    }

    public void setHeader(string nameObj, Type typeObj, Type typeCollectShallow = null)
    {
        pushNameObjNest(nameObj);
        PropertyInfo[] listPropInfo = typeObj.GetProperties();
        Type typeCollectNest = null;

        foreach (PropertyInfo propInfo in listPropInfo)
        {
            // Trace.WriteLine($"debug2 {propInfo.Name}");

            if (getBInclude(nameObj, propInfo.Name))
            {
                if (propInfo.PropertyType.GetInterface(nameof(ICollection)) != null)
                {
                    Type typeCollect = propInfo.PropertyType.GetGenericArguments().Single();

                    if(config.dictCollectRow.ContainsKey(nameObj) &&
                        config.dictCollectRow[nameObj] == propInfo.Name)
                    {
                        if (!getBTypeSystem(typeCollect))
                        {
                            // Trace.WriteLine("debug3");
                            typeCollectNest = typeCollect;
                            dictNameCollectNest[nameObjNest] = propInfo.Name;
                        }
                        else
                        {
                            // Trace.WriteLine("debug4");
                            bListSystem = true;
                        }
                    }
                    else if(!getBTypeSystem(typeCollect))
                    {
                        // Trace.WriteLine("debug5");
                        setHeader(propInfo.Name, typeCollect, propInfo.PropertyType);
                    }
                }
                else
                {
                    if (!getBTypeSystem(propInfo.PropertyType))
                    {
                        // Trace.WriteLine("debug6");
                        setHeader(propInfo.Name, propInfo.PropertyType);
                    }
                    else
                    {
                        // Trace.WriteLine("debug7");
                        listTypeObj.Add(typeCollectShallow == null ? typeObj : typeCollectShallow);
                        listNameObjNest.Add(nameObjNest);
                        listNameField.Add(propInfo.Name);

                        /*
                        colSort = config.listColConfig.Count > 0 ? config.listColConfig.FindIndex(j => j.name == propInfo.Name) : col;
                        ixlWorksheet.Cell(1, colSort + 1).Value = config.listColConfig.Count > 0 ? config.listColConfig[colSort].label : propInfo.Name;
                        */

                        if(config.listColConfig.Count == 0)
                        {
                            ixlWorksheet.Cell(1, col + 1).Value = propInfo.Name;
                            col++;
                        }
                    }
                }
            }
        }

        if(config.dictCollectRow.ContainsKey(nameObj))
        {
            if (typeCollectNest == null)
            {
                // Trace.WriteLine("debug8");
                listTypeObj.Add(typeObj);
                listNameObjNest.Add(nameObjNest);
                listNameField.Add(config.dictCollectRow[nameObj]);

                /*
                colSort = config.listColConfig.Count > 0 ? config.listColConfig.FindIndex(j => j.name == config.dictCollectRow[nameObj]) : col;
                ixlWorksheet.Cell(1, colSort + 1).Value = config.listColConfig.Count > 0 ? config.listColConfig[colSort].label : config.dictCollectRow[nameObj];
                */

                if(config.listColConfig.Count == 0)
                {
                    ixlWorksheet.Cell(config.rowStaHeader + 1, col + 1).Value = config.dictCollectRow[nameObj];
                    col++;
                }
            }
            else
            {
                // Trace.WriteLine("debug9");
                setHeader(config.dictCollectRow[nameObj], typeCollectNest);
            }
        }

        popNameObjNest();

        return;
    }

    public void setHeaderPredefine()
    {
        if(config.listColConfig.Count > 0)
        {
            for (int i = 0; i < config.listColConfig.Count; i++)
            {
                ixlWorksheet.Cell(config.rowStaHeader + 1, i + 1).Value = config.listColConfig[i].label ?? config.listColConfig[i].name;
            }
        }

        return;
    }

    public void formatCell(int i, object objProp)
    {
        Type typeObj = objProp.GetType();

        if (typeObj == typeof(int))
        {
            ixlWorksheet.Cell(row + 1, i + 1).Value = (int)objProp;
        }
        else if (typeObj == typeof(long))
        {
            ixlWorksheet.Cell(row + 1, i + 1).Value = (long)objProp;
        }
        else if (typeObj == typeof(double))
        {
            ixlWorksheet.Cell(row + 1, i + 1).Value = (double)objProp;
        }
        else if (typeObj == typeof(decimal))
        {
            ixlWorksheet.Cell(row + 1, i + 1).Value = (decimal)objProp;
        }
        else if(typeObj == typeof(DateTime))
        {
            if ((config.listColConfig.Count > 0) && config.listColConfig[i].bDateOnly)
            {
                if((((DateTime)objProp).Date == DateTime.MinValue.Date) ||
                    (((DateTime)objProp).Date == (new DateTime(2001, 1, 1))))
                {
                    ixlWorksheet.Cell(row + 1, i + 1).Value = "";
                }
                else
                {
                    ixlWorksheet.Cell(row + 1, i + 1).Value = ((DateTime)objProp).ToString("dd/MM/yyyy");
                }
            }
            else if ((config.listColConfig.Count > 0) && config.listColConfig[i].bTimeOnly)
            {
                ixlWorksheet.Cell(row + 1, i + 1).Value = ((DateTime)objProp).TimeOfDay == DateTime.MinValue.TimeOfDay ? "" : ((DateTime)objProp).ToString("HH:mm:ss");
            }
            else
            {
                ixlWorksheet.Cell(row + 1, i + 1).Value = (DateTime)objProp == DateTime.MinValue ? "" : (DateTime)objProp;
            }
        }
        else if(typeObj == typeof(TimeSpan))
        {
            ixlWorksheet.Cell(row + 1, i + 1).Value = (TimeSpan)objProp == DateTime.MinValue.TimeOfDay ? "" : (TimeSpan)objProp;
        }
        else
        {
            ixlWorksheet.Cell(row + 1, i + 1).Value =
                objProp == null ? "" :
                string.Format(config.listColConfig.Count > 0 ? config.listColConfig[i].formatStr : "{0}", objProp);
        }

        return;
    }

    public void setCell(int i)
    {
        listISort = config.listColConfig.Count > 0 ?
            Enumerable.Range(0, config.listColConfig.Count)
                .Where(j => config.listColConfig[j].name == listNameField[i] &&
                       (config.listColConfig[j].nameObj == null ||
                        config.listColConfig[j].nameObj == listNameObjNest[i].Split("___").Last()))
                .ToList()
            : new List<int> { i };

        object objNest = dictNameObjNest.ContainsKey(listNameObjNest[i]) ? dictNameObjNest[listNameObjNest[i]] : null;

        if (objNest == null)
        {
            //Trace.WriteLine($"[WARN] Object for {listNameObjNest[i]} is null, skipping setCell({i})");
            return;
        }

        if (listTypeObj[i].GetInterface(nameof(ICollection)) == null)
        {
            var propInfo = listTypeObj[i].GetProperty(listNameField[i]);
            if (propInfo == null)
            {
                //Trace.WriteLine($"[WARN] Property '{listNameField[i]}' not found in type '{listTypeObj[i]}'. Skipping.");
                return;
            }

            object objProp = propInfo.GetValue(objNest);
            foreach (int iSort in listISort)
            {
                // string strDebug = objProp == null ? "" : string.Format(config.listColConfig.Count > 0 ? config.listColConfig[iSort].formatStr : "{0}", objProp.ToString());
                // Trace.WriteLine($"debug25 {row + 1} {iSort + 1} {strDebug}");
                formatCell(iSort, objProp);
            }
        }
        else
        {
            var coll = objNest as ICollection;
            if (coll == null)
            {
                //Trace.WriteLine($"[WARN] Expected ICollection for {listNameObjNest[i]}, got null.");
                return;
            }

            Type typeCollectJoin = getTypeElement(coll);
            List<string> listPropJoin = new();

            if (config.dictMode.TryGetValue(ModeExcelWriter.sumCollect, out var sumFields) &&
                sumFields.Contains(listNameField[i]))
            {
                decimal sum = 0;
                foreach (var objCollect in coll)
                {
                    if (!getBIncludeRow(listNameObjNest[i].Split("___").Last(), objCollect)) continue;
                    var prop = typeCollectJoin.GetProperty(listNameField[i]);
                    var val = prop?.GetValue(objCollect);
                    sum += val == null ? 0 : Convert.ToDecimal(val);
                }

                foreach (int iSort in listISort)
                {
                    // Trace.WriteLine($"debug21 {row + 1} {iSort + 1} {string.Format(config.listColConfig[iSort].formatStr, sum.ToString())}");
                    ixlWorksheet.Cell(row + 1, iSort + 1).Value = sum;
                }
            }
            else
            {
                foreach (var objCollect in coll)
                {
                    if (!getBIncludeRow(listNameObjNest[i].Split("___").Last(), objCollect)) continue;
                    var prop = typeCollectJoin.GetProperty(listNameField[i]);
                    var val = prop?.GetValue(objCollect);
                    listPropJoin.Add(val?.ToString() ?? "");

                    if (config.dictMode.TryGetValue(ModeExcelWriter.firstCollect, out var firstFields) &&
                        firstFields.Contains(listNameField[i]))
                        break;
                }

                foreach (int iSort in listISort)
                {
                    // Trace.WriteLine($"debug22 {row + 1} {iSort + 1} {string.Format(config.listColConfig[iSort].formatStr, string.Join(",", listPropJoin))}");
                    
                    if((listPropJoin.Count == 1) && double.TryParse(listPropJoin[0], out double objDouble))
                    {
                        ixlWorksheet.Cell(row + 1, iSort + 1).Value = objDouble;
                    }
                    else
                    {
                        ixlWorksheet.Cell(row + 1, iSort + 1).Value = config.listColConfig.Count > 0 ? string.Format(config.listColConfig[iSort].formatStr, string.Join(",", listPropJoin)) : string.Join(",", listPropJoin);
                    }
                }
            }
        }
    }


    public void setValPredefine()
    {
        for (int i = 0; i < config.listColConfig.Count; i++)
        {
            if (config.listColConfig[i].val != null)
            {
                // Trace.WriteLine($"debug23 {row + 1} {i + 1} {config.listColConfig[i].val}");
                ixlWorksheet.Cell(row + 1, i + 1).Value = config.listColConfig[i].val;
            }
        }

        return;
    }

    public void setVal(string nameObj, object obj)
    {
        pushNameObjNest(nameObj);
        bSet = false;
        object listObjCollect = null;
        bool bSetGetObjAll = false;
        string nameCollect = null;
        dictNameObjNest[nameObjNest] = obj;

        if ((obj != null) && (obj.GetType().GetInterface(nameof(ICollection)) == null))
        {
            PropertyInfo[] listPropInfo = obj.GetType().GetProperties();

            foreach (PropertyInfo propInfo in listPropInfo)
            { 
                // Trace.WriteLine($"debug12 {propInfo.Name}");

                if(getBInclude(nameObj, propInfo.Name))
                {
                    if (propInfo.PropertyType.GetInterface(nameof(ICollection)) != null)
                    {
                        if (config.dictCollectRow.ContainsKey(nameObj) &&
                            (propInfo.Name == config.dictCollectRow[nameObj]))
                        {
                            // Trace.WriteLine($"debug13");
                            listObjCollect = propInfo.GetValue(obj);
                            nameCollect = propInfo.Name;
                        }
                        else
                        {
                            // Trace.WriteLine("debug14");
                            setVal(propInfo.Name, propInfo.GetValue(obj));
                        }
                    }
                    else if (!getBTypeSystem(propInfo.PropertyType))
                    {
                        // Trace.WriteLine("debug15");
                        setVal(propInfo.Name, propInfo.GetValue(obj));
                    }
                }
            }
        }

        if (listObjCollect != null)
        {
            if((getCount((ICollection)listObjCollect) == 0) && config.bAtLeastOneRow)
            {
                bSetGetObjAll = true;
            }
            else if (!listObjCollect.GetType().GetGenericArguments().Single().FullName.StartsWith("System."))
            {
                // Trace.WriteLine("debug16");
                foreach (object objCollect in (ICollection)listObjCollect)
                {
                    if (!getBIncludeRow(nameCollect, objCollect))
                    {
                        continue;
                    }

                    setVal(nameCollect, objCollect);
                }
            }
            else
            {
                // Trace.WriteLine("debug17");
                setVal(nameCollect, listObjCollect);
            }
        }

        bool bGetObjAll = bSetGetObjAll ? true : listNameObjNest.Distinct().All(i => dictNameObjNest.Keys.Contains(i));

        if (!bSet && bGetObjAll)
        {
            if(bListSystem)
            {
                if (getCount((ICollection)obj) > 0)
                {
                    foreach (object objCollect in (ICollection)obj)
                    {
                        // int iSort;

                        if (!getBIncludeRow(nameCollect, objCollect))
                        {
                            continue;
                        }

                        for (int i = 0; i < listTypeObj.Count; i++)
                        {
                            if (listTypeObj[i].GetInterface(nameof(ICollection)) != null)
                            {
                                // Trace.WriteLine($"debug18 {nameObj} {listNameField[i]}");
                                // iSort = config.listColConfig.Count > 0 ? config.listColConfig.FindIndex(j => j.name == listNameField[i]) : i;
                                listISort = config.listColConfig.Count > 0 ?
                                    Enumerable.Range(0, config.listColConfig.Count)
                                    .Where(j => (config.listColConfig[j].name == listNameField[i]) &&
                                    ((config.listColConfig[j].nameObj == null) ||
                                    (config.listColConfig[j].nameObj == nameObj)))
                                    .ToList() :
                                    new List<int> { i };

                                foreach (int iSort in listISort)
                                {
                                    // Trace.WriteLine($"debug24 {row + 1} {iSort + 1} {string.Format(config.listColConfig[iSort].formatStr, objCollect.ToString())}");
                                    formatCell(iSort, objCollect);
                                }
                            }
                            else if (!listTypeObj[i].FullName.StartsWith("System."))
                            {
                                // Trace.WriteLine("debug19");
                                setCell(i);
                            }
                        }

                        setValPredefine();
                        row++;
                    }
                }
                else if(config.bAtLeastOneRow)
                {
                    for (int i = 0; i < listTypeObj.Count; i++)
                    {
                        setCell(i);
                    }

                    setValPredefine();
                    row++;
                }
            }
            else
            {
                for (int i = 0; i < listTypeObj.Count; i++)
                {
                    // Trace.WriteLine("debug20");
                    setCell(i);
                }
                
                setValPredefine();
                row++;
            }

            bSet = true;
        }

        popNameObjNest();

        return;
    }

    public void setValExtra()
    {
        foreach(KeyValuePair<int, List<object>> kvp in config.dictColExtra)
        {
            for(int i = 0; i < kvp.Value.Count; i++)
            {
                // ixlWorksheet.Cell(i + config.rowStaContent + 1, kvp.Key + 1).Value = kvp.Value[i];
                row = i + config.rowStaContent;
                formatCell(kvp.Key, kvp.Value[i]);
            }
        }

        return;
    }

    public void setConst()
    {
        foreach(KeyValuePair<Tuple<int, int>, string> kvp in config.dictConst)
        {
            ixlWorksheet.Cell(kvp.Key.Item1 + 1, kvp.Key.Item2 + 1).Value = kvp.Value;
        }

        return;
    }

    public void formatCol()
    {
        // ixlWorksheet.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);

        for(int i = 0; i < config.listColConfig.Count(); i++)
        {
            if (config.listColConfig[i].bAdjust2Content)
            {
                ixlWorksheet.Column(i + 1).AdjustToContents();
            }
            else if (config.listColConfig[i].width > 0)
            {
                ixlWorksheet.Column(i + 1).Width = config.listColConfig[i].width;
            }

            if (config.listColConfig[i].bWrap)
            {
                ixlWorksheet.Column(i + 1).Style.Alignment.WrapText = true;
            }

            ixlWorksheet.Column(i + 1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);
        }

        return;
    }

    public async Task<IXLWorksheet> write(string fileName, ICollection listObj, ExcelWriterConfig _config = null, IXLWorksheet _ixlWorksheet = null, bool bWrite = true)
    {
        if (getCount(listObj) == 0)
        {
            JS.InvokeVoidAsync("alert", "No data can be downloaded as Excel file. Please check the checkbox on first column of table.");

            return null;
        }

        config = _config ?? new ExcelWriterConfig();

        if (_ixlWorksheet != null)
        {
            ixlWorksheet = _ixlWorksheet;
        }
        else
        {
            ixlWorksheet.Clear();
        }

        mergeCell();
        row = 0;
        col = 0;

        listTypeObj.Clear();
        nameObjNest = null;
        listNameObjNest.Clear();
        listNameField.Clear();

        bListSystem = false;
        dictNameCollectNest.Clear();
        
        setHeader("main", getTypeElement(listObj));
        setHeaderPredefine();
        row += config.rowStaContent;

        // Trace.WriteLine("\n\n");

        foreach (object obj in listObj)
        {
            nameObjNest = null;
            dictNameObjNest.Clear();
            bSet = false;

            setVal("main", obj);
            // setValPredefine();
        }

        setValExtra();
        setConst();
        formatCol();

        if (bWrite)
        {
            try
            {
                using MemoryStream memoryStream = new();
                xlWorkbook.SaveAs(memoryStream);
                await JS.InvokeAsync<object>("downloadFileFromBase64", fileName, Convert.ToBase64String(memoryStream.ToArray()));

            }
            catch (Exception e)
            {
                string message = $"Exception caught in {nameof(ExcelWriter)}.{nameof(write)}\n{e.Message}";
                Trace.WriteLine(message);
                await JS.InvokeVoidAsync("alert", message);
            }
        }

        return ixlWorksheet;
    }
}

public enum ModeExcelWriter
{
    firstCollect,
    sumCollect
}

public class ExcelColConfig
{
    public string name;
    public string nameObj = null;
    public string label = null;
    public string val = null;
    public string formatStr = "{0}";
    public bool bDateOnly = false;
    public bool bTimeOnly = false;
    public bool bAdjust2Content = false;
    public int width = 0;
    public bool bWrap = false;

    public ExcelColConfig(string _name)
    {
        name = _name;

        return;
    }
}

public class ExcelWriterConfig
{
    // Key is object name, value is collection property names which the items should be placed in separated rows
    public Dictionary<string, string> dictCollectRow;
    // Key is object name, value is list of property names to be ignored
    public Dictionary<string, List<string>> dictIgnore;
    // Key is object name, value is list of property names to be included
    // If dictIgnore is specified, dictInclude is ignored
    public Dictionary<string, List<string>> dictInclude;
    // Value is list of property names
    public Dictionary<ModeExcelWriter, List<string>> dictMode;
    // List of configurations of columns in sequence, currently not supporting duplicated column names
    public List<ExcelColConfig> listColConfig;
    // Key is object name, value is property name used to determine whether a row should be included
    public Dictionary<string, string> dictPropBInclude;
    // Key is column index, value is list of extra properties to be included in the column
    public Dictionary<int, List<object>> dictColExtra;
    // Key is row and column, value is constant value to be placed in the cell
    public Dictionary<Tuple<int, int>, string> dictConst;
    public List<Tuple<int, int, int, int>> listMerge;

    public int rowStaHeader;
    public int rowStaContent;
    public string nameFileDefault;
    public bool bAtLeastOneRow;

    public ExcelWriterConfig()
    {
        dictCollectRow = new Dictionary<string, string>();
        dictIgnore = new Dictionary<string, List<string>>();
        dictInclude = new Dictionary<string, List<string>>();
        dictMode = new Dictionary<ModeExcelWriter, List<string>>();
        listColConfig = new List<ExcelColConfig>();
        dictPropBInclude = new Dictionary<string, string>();
        dictColExtra = new Dictionary<int, List<object>>();
        dictConst = new Dictionary<Tuple<int, int>, string>();
        listMerge = new List<Tuple<int, int, int, int>>();

        rowStaHeader = 0;
        rowStaContent = 1;
        nameFileDefault = null;
        bAtLeastOneRow = false;

        return;
    }
}

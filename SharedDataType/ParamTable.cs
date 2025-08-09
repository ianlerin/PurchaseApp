namespace SharedDataType
{
    public class TextFilter
    {
        public string selectedItem = null;
        public string subText = null;
    }

    public class DateFilter
    {
        public DateTime? startDate = null;
        public DateTime? endDate = null;
    }

    public class NumberFilter
    {
        public double lowerBound = 0;
        public double upperBound = 0;
    }

    public class TimeFilter
    {
        public TimeSpan startTime = TimeSpan.Zero;
        public TimeSpan endTime = TimeSpan.Zero;
    }

    public class TableOrder
    {
        public string table { get; set; }
        public string col { get; set; }
        public bool bAsc { get; set; }

        public TableOrder()
        {
            table = "";
            col = "";
            bAsc = true;

            return;
        }
    }

    public class ParamSearch
    {
        public Dictionary<string, object> dictFilter { get; set; }
        public int firstRowIndex { get; set; }
        public TableOrder tableOrder { get; set; }

        public ParamSearch()
        {
            dictFilter = new Dictionary<string, object>();
            firstRowIndex = 0;
            tableOrder = new TableOrder();

            return;
        }
    }

    public class ContentTable
    {
        public List<Dictionary<string, object>> listDbRow { get; set; }
        public List<string> listColFlat { get; set; }
        public int totalRow { get; set; }
        public int rowPerPage { get; set; }

        public ContentTable()
        {
            listDbRow = new List<Dictionary<string, object>>();
            listColFlat = new List<string>();
            totalRow = 0;
            rowPerPage = 0;

            return;
        }
    }
}

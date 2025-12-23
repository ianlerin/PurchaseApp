using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Graph.Models;
using PurchaseBlazorApp2.Client.Pages.HR;
using WorkerRecord;

namespace PurchaseBlazorApp2.Resource
{
    public interface IEPFTableService
    {
        IReadOnlyList<ContributionRange> GetTableA();
        IReadOnlyList<ContributionRange> GetTableC();
        IReadOnlyList<ContributionRange> GetTableE();
        public (decimal Employer, decimal Employee) UpdateWageInfo(SingleWageRecord Record);
    }
    public class EPFExcelConverter : IEPFTableService
    {
        private readonly IReadOnlyList<ContributionRange> EPFAList;
        private readonly IReadOnlyList<ContributionRange> EPFCList;
        private readonly IReadOnlyList<ContributionRange> EPFEList;
        public EPFExcelConverter()
        {
            string EPFAPath= Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "EPFTableA.xlsx"
    );
            string EPFCPath = Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "EPFTableC.xlsx"
    );
            string EPFEPath = Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "EPFTableE.xlsx"
    );
            EPFAList = ReadContributionRanges(EPFAPath);
            EPFCList = ReadContributionRanges(EPFCPath);
            EPFEList = ReadContributionRanges(EPFEPath);
        }

        public IReadOnlyList<ContributionRange> GetTableA()
        {
            return EPFAList;
        }

        public IReadOnlyList<ContributionRange> GetTableC()
        {
            return EPFCList;
        }

        public IReadOnlyList<ContributionRange> GetTableE()
        {
            return EPFEList;
        }

        private List<ContributionRange> ReadContributionRanges(string filePath)
        {
            var result = new List<ContributionRange>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);

            foreach (var row in worksheet.RowsUsed())
            {
                // Column A must start with "From"
                var firstCell = row.Cell(1).GetString();
                if (!firstCell.Equals("From", StringComparison.OrdinalIgnoreCase))
                    continue;

                var item = new ContributionRange
                {
                    From = ParseDecimalOrZero(row.Cell(2).GetString()),
                    To = ParseDecimalOrZero(row.Cell(4).GetString()),
                    EmployerContribute = ParseDecimalOrZero(row.Cell(5).GetString()),
                    EmployeeContribute = ParseDecimalOrZero(row.Cell(6).GetString())
                };

                result.Add(item);
            }

            return result;
        }

        private (decimal Employer, decimal Employee) GetContributions(List<ContributionRange> ranges, decimal wage)
        {
            if (ranges == null || ranges.Count == 0)
                return (0m, 0m);

            int left = 0;
            int right = ranges.Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                var range = ranges[mid];

                if (wage >= range.From && wage <= range.To)
                {
                    return (range.EmployerContribute, range.EmployeeContribute);
                }
                else if (wage < range.From)
                {
                    right = mid - 1;
                }
                else 
                {
                    left = mid + 1;
                }
            }

            return (0m, 0m);
        }

        public  (decimal Employer, decimal Employee) UpdateWageInfo(SingleWageRecord Record)
        {
            List<ContributionRange> Range = new List<ContributionRange>();
            switch (Record.EPFCategory)
            {
                case (EEPFCategory.A):
                    Range = EPFAList.ToList();
                    break;
                case (EEPFCategory.C):
                    Range = EPFCList.ToList();
                    break;
                case (EEPFCategory.E):
                    Range = EPFEList.ToList();
                    break;
            }
            return GetContributions(Range, Record.Total_wages);
        
        }

        private decimal ParseDecimalOrZero(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            if (value.Trim().Equals("NIL", StringComparison.OrdinalIgnoreCase))
                return 0m;

            return decimal.Parse(value);
        }
    }
}

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
        public (decimal Employer, decimal Employee) UpdateEPFWageInfo(SingleWageRecord Record);
        public (decimal Employer, decimal Employee) UpdateSocsoWageInfo(SingleWageRecord Record);
    }
    public class EPFExcelConverter : IEPFTableService
    {

        public  readonly List<ContributionRange> SoscoAct800ContributionRanges = new()
{
    new ContributionRange { From = 0, To = 30, EmployerContribute = 0.05m, EmployeeContribute = 0.05m },
    new ContributionRange { From = 30, To = 50, EmployerContribute = 0.10m, EmployeeContribute = 0.10m },
    new ContributionRange { From = 50, To = 70, EmployerContribute = 0.15m, EmployeeContribute = 0.15m },
    new ContributionRange { From = 70, To = 100, EmployerContribute = 0.20m, EmployeeContribute = 0.20m },
    new ContributionRange { From = 100, To = 140, EmployerContribute = 0.25m, EmployeeContribute = 0.25m },
    new ContributionRange { From = 140, To = 200, EmployerContribute = 0.35m, EmployeeContribute = 0.35m },
    new ContributionRange { From = 200, To = 300, EmployerContribute = 0.50m, EmployeeContribute = 0.50m },
    new ContributionRange { From = 300, To = 400, EmployerContribute = 0.70m, EmployeeContribute = 0.70m },
    new ContributionRange { From = 400, To = 500, EmployerContribute = 0.90m, EmployeeContribute = 0.90m },
    new ContributionRange { From = 500, To = 600, EmployerContribute = 1.10m, EmployeeContribute = 1.10m },
    new ContributionRange { From = 600, To = 700, EmployerContribute = 1.30m, EmployeeContribute = 1.30m },
    new ContributionRange { From = 700, To = 800, EmployerContribute = 1.50m, EmployeeContribute = 1.50m },
    new ContributionRange { From = 800, To = 900, EmployerContribute = 1.70m, EmployeeContribute = 1.70m },
    new ContributionRange { From = 900, To = 1000, EmployerContribute = 1.90m, EmployeeContribute = 1.90m },
    new ContributionRange { From = 1000, To = 1100, EmployerContribute = 2.10m, EmployeeContribute = 2.10m },
    new ContributionRange { From = 1100, To = 1200, EmployerContribute = 2.30m, EmployeeContribute = 2.30m },
    new ContributionRange { From = 1200, To = 1300, EmployerContribute = 2.50m, EmployeeContribute = 2.50m },
    new ContributionRange { From = 1300, To = 1400, EmployerContribute = 2.70m, EmployeeContribute = 2.70m },
    new ContributionRange { From = 1400, To = 1500, EmployerContribute = 2.90m, EmployeeContribute = 2.90m },
    new ContributionRange { From = 1500, To = 1600, EmployerContribute = 3.10m, EmployeeContribute = 3.10m },
    new ContributionRange { From = 1600, To = 1700, EmployerContribute = 3.30m, EmployeeContribute = 3.30m },
    new ContributionRange { From = 1700, To = 1800, EmployerContribute = 3.50m, EmployeeContribute = 3.50m },
    new ContributionRange { From = 1800, To = 1900, EmployerContribute = 3.70m, EmployeeContribute = 3.70m },
    new ContributionRange { From = 1900, To = 2000, EmployerContribute = 3.90m, EmployeeContribute = 3.90m },
    new ContributionRange { From = 2000, To = 2100, EmployerContribute = 4.10m, EmployeeContribute = 4.10m },
    new ContributionRange { From = 2100, To = 2200, EmployerContribute = 4.30m, EmployeeContribute = 4.30m },
    new ContributionRange { From = 2200, To = 2300, EmployerContribute = 4.50m, EmployeeContribute = 4.50m },
    new ContributionRange { From = 2300, To = 2400, EmployerContribute = 4.70m, EmployeeContribute = 4.70m },
    new ContributionRange { From = 2400, To = 2500, EmployerContribute = 4.90m, EmployeeContribute = 4.90m },
    new ContributionRange { From = 2500, To = 2600, EmployerContribute = 5.10m, EmployeeContribute = 5.10m },
    new ContributionRange { From = 2600, To = 2700, EmployerContribute = 5.30m, EmployeeContribute = 5.30m },
    new ContributionRange { From = 2700, To = 2800, EmployerContribute = 5.50m, EmployeeContribute = 5.50m },
    new ContributionRange { From = 2800, To = 2900, EmployerContribute = 5.70m, EmployeeContribute = 5.70m },
    new ContributionRange { From = 2900, To = 3000, EmployerContribute = 5.90m, EmployeeContribute = 5.90m },
    new ContributionRange { From = 3000, To = 3100, EmployerContribute = 6.10m, EmployeeContribute = 6.10m },
    new ContributionRange { From = 3100, To = 3200, EmployerContribute = 6.30m, EmployeeContribute = 6.30m },
    new ContributionRange { From = 3200, To = 3300, EmployerContribute = 6.50m, EmployeeContribute = 6.50m },
    new ContributionRange { From = 3300, To = 3400, EmployerContribute = 6.70m, EmployeeContribute = 6.70m },
    new ContributionRange { From = 3400, To = 3500, EmployerContribute = 6.90m, EmployeeContribute = 6.90m },
    new ContributionRange { From = 3500, To = 3600, EmployerContribute = 7.10m, EmployeeContribute = 7.10m },
    new ContributionRange { From = 3600, To = 3700, EmployerContribute = 7.30m, EmployeeContribute = 7.30m },
    new ContributionRange { From = 3700, To = 3800, EmployerContribute = 7.50m, EmployeeContribute = 7.50m },
    new ContributionRange { From = 3800, To = 3900, EmployerContribute = 7.70m, EmployeeContribute = 7.70m },
    new ContributionRange { From = 3900, To = 4000, EmployerContribute = 7.90m, EmployeeContribute = 7.90m },
    new ContributionRange { From = 4000, To = 4100, EmployerContribute = 8.10m, EmployeeContribute = 8.10m },
    new ContributionRange { From = 4100, To = 4200, EmployerContribute = 8.30m, EmployeeContribute = 8.30m },
    new ContributionRange { From = 4200, To = 4300, EmployerContribute = 8.50m, EmployeeContribute = 8.50m },
    new ContributionRange { From = 4300, To = 4400, EmployerContribute = 8.70m, EmployeeContribute = 8.70m },
    new ContributionRange { From = 4400, To = 4500, EmployerContribute = 8.90m, EmployeeContribute = 8.90m },
    new ContributionRange { From = 4500, To = 4600, EmployerContribute = 9.10m, EmployeeContribute = 9.10m },
    new ContributionRange { From = 4600, To = 4700, EmployerContribute = 9.30m, EmployeeContribute = 9.30m },
    new ContributionRange { From = 4700, To = 4800, EmployerContribute = 9.50m, EmployeeContribute = 9.50m },
    new ContributionRange { From = 4800, To = 4900, EmployerContribute = 9.70m, EmployeeContribute = 9.70m },
    new ContributionRange { From = 4900, To = 5000, EmployerContribute = 9.90m, EmployeeContribute = 9.90m },
    new ContributionRange { From = 5000, To = 5100, EmployerContribute = 10.10m, EmployeeContribute = 10.10m },
    new ContributionRange { From = 5100, To = 5200, EmployerContribute = 10.30m, EmployeeContribute = 10.30m },
    new ContributionRange { From = 5200, To = 5300, EmployerContribute = 10.50m, EmployeeContribute = 10.50m },
    new ContributionRange { From = 5300, To = 5400, EmployerContribute = 10.70m, EmployeeContribute = 10.70m },
    new ContributionRange { From = 5400, To = 5500, EmployerContribute = 10.90m, EmployeeContribute = 10.90m },
    new ContributionRange { From = 5500, To = 5600, EmployerContribute = 11.10m, EmployeeContribute = 11.10m },
    new ContributionRange { From = 5600, To = 5700, EmployerContribute = 11.30m, EmployeeContribute = 11.30m },
    new ContributionRange { From = 5700, To = 5800, EmployerContribute = 11.50m, EmployeeContribute = 11.50m },
    new ContributionRange { From = 5800, To = 5900, EmployerContribute = 11.70m, EmployeeContribute = 11.70m },
    new ContributionRange { From = 5900, To = 6000, EmployerContribute = 11.90m, EmployeeContribute = 11.90m },
    new ContributionRange { From = 6000, To = decimal.MaxValue, EmployerContribute = 11.90m, EmployeeContribute = 11.90m }
};

        public static readonly List<ContributionRange> SoscoAct4ContributionRanges = new()
{
    new() { From = 0, To = 30, EmployerContribute = 0.30m, EmployeeContribute = 0.10m },
    new() { From = 30, To = 50, EmployerContribute = 0.50m, EmployeeContribute = 0.20m },
    new() { From = 50, To = 70, EmployerContribute = 0.80m, EmployeeContribute = 0.30m },
    new() { From = 70, To = 100, EmployerContribute = 1.10m, EmployeeContribute = 0.40m },
    new() { From = 100, To = 140, EmployerContribute = 1.50m, EmployeeContribute = 0.60m },
    new() { From = 140, To = 200, EmployerContribute = 2.10m, EmployeeContribute = 0.85m },
    new() { From = 200, To = 300, EmployerContribute = 3.10m, EmployeeContribute = 1.25m },
    new() { From = 300, To = 400, EmployerContribute = 4.40m, EmployeeContribute = 1.75m },
    new() { From = 400, To = 500, EmployerContribute = 5.60m, EmployeeContribute = 2.25m },
    new() { From = 500, To = 600, EmployerContribute = 6.90m, EmployeeContribute = 2.75m },
    new() { From = 600, To = 700, EmployerContribute = 8.10m, EmployeeContribute = 3.25m },
    new() { From = 700, To = 800, EmployerContribute = 9.40m, EmployeeContribute = 3.75m },
    new() { From = 800, To = 900, EmployerContribute = 10.60m, EmployeeContribute = 4.25m },
    new() { From = 900, To = 1000, EmployerContribute = 11.90m, EmployeeContribute = 4.75m },
    new() { From = 1000, To = 1100, EmployerContribute = 13.10m, EmployeeContribute = 5.25m },
    new() { From = 1100, To = 1200, EmployerContribute = 14.40m, EmployeeContribute = 5.75m },
    new() { From = 1200, To = 1300, EmployerContribute = 15.60m, EmployeeContribute = 6.25m },
    new() { From = 1300, To = 1400, EmployerContribute = 16.90m, EmployeeContribute = 6.75m },
    new() { From = 1400, To = 1500, EmployerContribute = 18.10m, EmployeeContribute = 7.25m },
    new() { From = 1500, To = 1600, EmployerContribute = 19.40m, EmployeeContribute = 7.75m },
    new() { From = 1600, To = 1700, EmployerContribute = 20.60m, EmployeeContribute = 8.25m },
    new() { From = 1700, To = 1800, EmployerContribute = 21.90m, EmployeeContribute = 8.75m },
    new() { From = 1800, To = 1900, EmployerContribute = 23.10m, EmployeeContribute = 9.25m },
    new() { From = 1900, To = 2000, EmployerContribute = 24.40m, EmployeeContribute = 9.75m },
    new() { From = 2000, To = 2100, EmployerContribute = 25.60m, EmployeeContribute = 10.25m },
    new() { From = 2100, To = 2200, EmployerContribute = 26.90m, EmployeeContribute = 10.75m },
    new() { From = 2200, To = 2300, EmployerContribute = 28.10m, EmployeeContribute = 11.25m },
    new() { From = 2300, To = 2400, EmployerContribute = 29.40m, EmployeeContribute = 11.75m },
    new() { From = 2400, To = 2500, EmployerContribute = 30.60m, EmployeeContribute = 12.25m },
    new() { From = 2500, To = 2600, EmployerContribute = 31.90m, EmployeeContribute = 12.75m },
    new() { From = 2600, To = 2700, EmployerContribute = 33.10m, EmployeeContribute = 13.25m },
    new() { From = 2700, To = 2800, EmployerContribute = 34.40m, EmployeeContribute = 13.75m },
    new() { From = 2800, To = 2900, EmployerContribute = 35.60m, EmployeeContribute = 14.25m },
    new() { From = 2900, To = 3000, EmployerContribute = 36.90m, EmployeeContribute = 14.75m },
    new() { From = 3000, To = 3100, EmployerContribute = 38.10m, EmployeeContribute = 15.25m },
    new() { From = 3100, To = 3200, EmployerContribute = 39.40m, EmployeeContribute = 15.75m },
    new() { From = 3200, To = 3300, EmployerContribute = 40.60m, EmployeeContribute = 16.25m },
    new() { From = 3300, To = 3400, EmployerContribute = 41.90m, EmployeeContribute = 16.75m },
    new() { From = 3400, To = 3500, EmployerContribute = 43.10m, EmployeeContribute = 17.25m },
    new() { From = 3500, To = 3600, EmployerContribute = 44.40m, EmployeeContribute = 17.75m },
    new() { From = 3600, To = 3700, EmployerContribute = 45.60m, EmployeeContribute = 18.25m },
    new() { From = 3700, To = 3800, EmployerContribute = 46.90m, EmployeeContribute = 18.75m },
    new() { From = 3800, To = 3900, EmployerContribute = 48.10m, EmployeeContribute = 19.25m },
    new() { From = 3900, To = 4000, EmployerContribute = 49.40m, EmployeeContribute = 19.75m },
    new() { From = 4000, To = 4100, EmployerContribute = 50.60m, EmployeeContribute = 20.25m },
    new() { From = 4100, To = 4200, EmployerContribute = 51.90m, EmployeeContribute = 20.75m },
    new() { From = 4200, To = 4300, EmployerContribute = 53.10m, EmployeeContribute = 21.25m },
    new() { From = 4300, To = 4400, EmployerContribute = 54.40m, EmployeeContribute = 21.75m },
    new() { From = 4400, To = 4500, EmployerContribute = 55.60m, EmployeeContribute = 22.25m },
    new() { From = 4500, To = 4600, EmployerContribute = 56.90m, EmployeeContribute = 22.75m },
    new() { From = 4600, To = 4700, EmployerContribute = 58.10m, EmployeeContribute = 23.25m },
    new() { From = 4700, To = 4800, EmployerContribute = 59.40m, EmployeeContribute = 23.75m },
    new() { From = 4800, To = 4900, EmployerContribute = 60.60m, EmployeeContribute = 24.25m },
    new() { From = 4900, To = 5000, EmployerContribute = 61.90m, EmployeeContribute = 24.75m },
    new() { From = 5000, To = 5100, EmployerContribute = 63.10m, EmployeeContribute = 25.25m },
    new() { From = 5100, To = 5200, EmployerContribute = 64.40m, EmployeeContribute = 25.75m },
    new() { From = 5200, To = 5300, EmployerContribute = 65.60m, EmployeeContribute = 26.25m },
    new() { From = 5300, To = 5400, EmployerContribute = 66.90m, EmployeeContribute = 26.75m },
    new() { From = 5400, To = 5500, EmployerContribute = 68.10m, EmployeeContribute = 27.25m },
    new() { From = 5500, To = 5600, EmployerContribute = 69.40m, EmployeeContribute = 27.75m },
    new() { From = 5600, To = 5700, EmployerContribute = 70.60m, EmployeeContribute = 28.25m },
    new() { From = 5700, To = 5800, EmployerContribute = 71.90m, EmployeeContribute = 28.75m },
    new() { From = 5800, To = 5900, EmployerContribute = 73.10m, EmployeeContribute = 29.25m },
    new() { From = 5900, To = 6000, EmployerContribute = 74.40m, EmployeeContribute = 29.75m },
    new() { From = 6000, To = decimal.MaxValue, EmployerContribute = 74.40m, EmployeeContribute = 29.75m }
};


        private readonly IReadOnlyList<ContributionRange> EPFAList;
        private readonly IReadOnlyList<ContributionRange> EPFCList;
        private readonly IReadOnlyList<ContributionRange> EPFEList;
        public EPFExcelConverter()
        {
            string EPFAPath= Path.Combine(
        AppContext.BaseDirectory,
        "Resource",
        "EPFTableA.xlsx"
    );
            string EPFCPath = Path.Combine(
        AppContext.BaseDirectory,
        "Resource",
        "EPFTableC.xlsx"
    );
            string EPFEPath = Path.Combine(
        AppContext.BaseDirectory,
        "Resource",
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

        public (decimal Employer, decimal Employee) UpdateSocsoWageInfo(SingleWageRecord Record)
        {

            switch (Record.SocsoCategory)
            {
                case (ESocsoCategory.Act4):
                    return GetContributions(SoscoAct4ContributionRanges, Record.Gross_wages);

                case (ESocsoCategory.Act800):
                    return GetContributions(SoscoAct800ContributionRanges, Record.Gross_wages);
            }
            return (0, 0);
        }

        public (decimal Employer, decimal Employee) UpdateEPFWageInfo(SingleWageRecord Record)
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
                case (EEPFCategory.F):
                    return EPFCategoryFCalculation(Record);
            }
            return GetContributions(Range, Record.EPF);
        
        }
        private (decimal Employer, decimal Employee) EPFCategoryFCalculation(SingleWageRecord Record)
        {
            decimal contribution = Math.Ceiling(Record.Gross_wages * 0.02m);
            return (contribution, contribution);
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

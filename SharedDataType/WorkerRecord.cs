using PurchaseBlazorApp2.Components.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WorkerRecord
{
    public enum EWorkerStatus
    {
        Active,Inactive,All
    }
    public enum EEPFCategory
    {
        A,B,C,D,E
    }
    public enum ENationalityStatus
    {
        Foreign,Local
    }

    public class GenerateWagesPdfRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public UserName MyUser { get; set; }
    }
    public class WorkerRecord
    {
        public string? ID {  get; set; }
        public string? Name { get; set; }
        public EEPFCategory EPFStatus { get; set; }
        public ENationalityStatus NationalityStatus { get; set; }
        public decimal Age { get; set; }

        public string? Passport { get; set; }
        public decimal DailyRate { get; set; }
        public decimal OTRate { get; set; }

        public decimal SundayRate { get; set; }
        public decimal MonthlyRate { get; set; }
        public decimal HourlyRate { get; set; }
        public EWorkerStatus WorkerStatus { get; set; }
    }

        public class WageRecord
        {
            public int Year { get; set; }
            public int Month { get; set; }

            public List<SingleWageRecord>  WageRecords{get;set;}
            public void FormWageRecordFromWorkerRecords(List<WorkerRecord> records)
            {
                if (records == null || records.Count == 0)
                {
                    WageRecords = new List<SingleWageRecord>();
                    return;
                }

                WageRecords = records.Select(worker => new SingleWageRecord
                {
                    ID = worker.ID,
                    Name = worker.Name,
                    // Initialize hours to 0
                    DailyHours = 0,
                    OTHours = 0,
                    SundayHours = 0,
                    MonthlyHours = 0,
                    HourlyHours = 0,

                    // Copy rates from worker record
                    DailyRate = worker.DailyRate,
                    OTRate = worker.OTRate,
                    SundayRate = worker.SundayRate,
                    MonthlyRate = worker.MonthlyRate,
                    HourlyRate = worker.HourlyRate
                }).ToList();
            }
            public WageRecord()
        {
            WageRecords = new List<SingleWageRecord>();
            Year =DateTime.Now.Year;
                Month=DateTime.Now.Month;
            }
        }

        public class SingleWageRecord
        {
            public string? ID { get; set; }
            public string? Name { get; set; }
            // --- Hours ---
            private decimal _DailyHours;
            public decimal DailyHours
            {
                get => _DailyHours;
                set { _DailyHours = value; RecalculateWages(); }
            }

            private decimal _OTHours;
            public decimal OTHours
            {
                get => _OTHours;
                set { _OTHours = value; RecalculateWages(); }
            }

            private decimal _SundayHours;
            public decimal SundayHours
            {
                get => _SundayHours;
                set { _SundayHours = value; RecalculateWages(); }
            }

            private decimal _MonthlyHours;
            public decimal MonthlyHours
            {
                get => _MonthlyHours;
                set { _MonthlyHours = value; RecalculateWages(); }
            }

            private decimal _HourlyHours;
            public decimal HourlyHours
            {
                get => _HourlyHours;
                set { _HourlyHours = value; RecalculateWages(); }
            }


            // --- Rates ---
            private decimal _DailyRate;
            public decimal DailyRate
            {
                get => _DailyRate;
                set { _DailyRate = value; RecalculateWages(); }
            }

            private decimal _OTRate;
            public decimal OTRate
            {
                get => _OTRate;
                set { _OTRate = value; RecalculateWages(); }
            }

            private decimal _SundayRate;
            public decimal SundayRate
            {
                get => _SundayRate;
                set { _SundayRate = value; RecalculateWages(); }
            }

            private decimal _MonthlyRate;
            public decimal MonthlyRate
            {
                get => _MonthlyRate;
                set { _MonthlyRate = value; RecalculateWages(); }
            }

            private decimal _HourlyRate;
            public decimal HourlyRate
            {
                get => _HourlyRate;
                set { _HourlyRate = value; RecalculateWages(); }
            }


            // --- Calculated Wages ---
            private decimal _Daily_wages;
            public decimal Daily_wages
            {
                get => _Daily_wages;
                 set { _Daily_wages = value; }
            }

            private decimal _OT_wages;
            public decimal OT_wages
            {
                get => _OT_wages;
                 set { _OT_wages = value; }
            }

            private decimal _Sunday_wages;
            public decimal Sunday_wages
            {
                get => _Sunday_wages;
                 set { _Sunday_wages = value; }
            }

            private decimal _Monthly_wages;
            public decimal Monthly_wages
            {
                get => _Monthly_wages;
                 set { _Monthly_wages = value; }
            }

            private decimal _Hourly_wages;
            public decimal Hourly_wages
            {
                get => _Hourly_wages;
                 set { _Hourly_wages = value; }
            }


            private void RecalculateWages()
            {
                Daily_wages = DailyHours * DailyRate;
                OT_wages = OTHours * OTRate;
                Sunday_wages = SundayHours * SundayRate;
                Monthly_wages = MonthlyHours * MonthlyRate;
                Hourly_wages = HourlyHours * HourlyRate;
                OnRecalculateTotalPrice();
            }

            public decimal Total_wages { get; set; }

            private void OnRecalculateTotalPrice()
            {
                Total_wages = Daily_wages + OT_wages + Sunday_wages + Monthly_wages + Hourly_wages;
            }

        }

 
}

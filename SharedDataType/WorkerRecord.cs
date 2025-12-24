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
        Active,Inactive, All
    }
    public enum EEPFCategory
    {
        A, C, E, F
    }

    public enum ESocsoCategory
    {
        Act4, Act800
    }
    public enum ENationalityStatus
    {
        Foreign,Local,PR
    }
    public class ContributeResult
    {
        public decimal EPFEmployerContribution { get; set; }
        public decimal EPFEmployeeContribution { get; set; }
        public decimal SocsoEmployerContribution { get; set; }
        public decimal SocsoEmployeeContribution { get; set; }
    }
    public class ContributionRange
    {
        public decimal From { get; set; }
        public decimal To { get; set; }
        public decimal EmployerContribute { get; set; }
        public decimal EmployeeContribute { get; set; }

     
    }


    public class GenerateWagesPdfRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public UserName MyUser { get; set; }
    }

    public class WorkerPdfRequest
    {
        public string FilterStatus { get; set; }
    }

    public class WorkerRecord
    {
        public bool IsLoading = false;
        public string? ID { get; set; }
        public string? Name { get; set; }

        private EEPFCategory _epfStatus;
        public EEPFCategory EPFStatus
        {
            get => _epfStatus;
            set => _epfStatus = value; 
        }
        private ESocsoCategory _SocsoCategory;
        public ESocsoCategory SocsoCategory
        {
            get => _SocsoCategory;
            set => _SocsoCategory = value;
        }

        

        private ENationalityStatus _nationalityStatus;
        public ENationalityStatus NationalityStatus
        {
            get => _nationalityStatus;
            set
            {
                if (_nationalityStatus != value)
                {
                    _nationalityStatus = value;
                    UpdateEPFStatus();
                }
            }
        }

        private decimal _age;
        public decimal Age
        {
            get => _age;
            set
            {
                if (_age != value)
                {
                    _age = value;
                    UpdateEPFStatus();
                }
            }
        }
        private decimal _MonthlyRate;
        public decimal MonthlyRate
        {
            get => _MonthlyRate;
            set
            {
                if (_MonthlyRate != value)
                {
                    _MonthlyRate = value;
                    AutoComputeWagesBasedOnMonthly();
                    UpdateEPFStatus();
                }
            }
        }

        public string? Passport { get; set; }
        public decimal DailyRate { get; set; }
        public decimal OTRate { get; set; }
        public decimal SundayRate { get; set; }
        public decimal HourlyRate { get; set; }
        public EWorkerStatus WorkerStatus { get; set; }

        private void UpdateEPFStatus()
        {
            if(IsLoading)
            {
                return;
            }
            EPFStatus = AutoDetectEPFStatus();
            SocsoCategory = AutoDetectSocsoStatus();
        }

        private EEPFCategory AutoDetectEPFStatus()
        {
            if (NationalityStatus != ENationalityStatus.Foreign && Age < 60)
                return EEPFCategory.A;

            if (NationalityStatus == ENationalityStatus.Local && Age >= 60)
                return EEPFCategory.E;

            if (NationalityStatus == ENationalityStatus.PR && Age >= 60)
                return EEPFCategory.C;

            return EEPFCategory.F;
        }

        private ESocsoCategory AutoDetectSocsoStatus()
        {
            if(NationalityStatus==ENationalityStatus.Local)
            {
                return ESocsoCategory.Act800;
            }
            return ESocsoCategory.Act4;
        }

        private void AutoComputeWagesBasedOnMonthly()
        {
            if (IsLoading || MonthlyRate <= 0)
                return;

            DailyRate = Math.Round(MonthlyRate / 26m, 2);

            OTRate = Math.Round((DailyRate / 8m) * 1.5m, 2);

            SundayRate = Math.Round((DailyRate / 8m) * 2m, 2);
        }
    }

    public class WageRecord
    {
        //public EventHandler EPFRecalculateHandler;
      
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

            WageRecords = new List<SingleWageRecord>();

            for (int i = 0; i < records.Count; i++)
            {
                var worker = records[i];
                var wageRecord = new SingleWageRecord
                {
                    ID = worker.ID,
                    Name = worker.Name,
                    // Initialize hours to 0
                    DailyHours = 0,
                    OTHours = 0,
                    SundayHours = 0,
                    MonthlyHours = 0,
                    HourlyHours = 0,
                    EPFCategory = worker.EPFStatus,
                    SocsoCategory = worker.SocsoCategory,
                    // Copy rates from worker record
                    DailyRate = worker.DailyRate,
                    OTRate = worker.OTRate,
                    SundayRate = worker.SundayRate,
                    MonthlyRate = worker.MonthlyRate,
                    HourlyRate = worker.HourlyRate
                };

                AddRecord(wageRecord);
            }
        }

        public void AddRecord(SingleWageRecord wageRecord)
        {
            //wageRecord.EPFRecalculateHandler += OnRecalculateEPF;
            WageRecords.Add(wageRecord);
        }
        /*
        private void OnRecalculateEPF(object o, EventArgs e)
        {
            EPFRecalculateHandler?.Invoke(this, EventArgs.Empty);
        }
        */
        public WageRecord()
        {
            WageRecords = new List<SingleWageRecord>();
            Year =DateTime.Now.Year;
                Month=DateTime.Now.Month;
            }
        }

        public class SingleWageRecord
        {

        public bool IsLoading = false;
        public EventHandler EPFRecalculateHandler;
            public string? ID { get; set; }
            public string? Name { get; set; }
            public string? Deduction_Reason { get; set;}
        private EEPFCategory _EPFCategory;
        public EEPFCategory EPFCategory
        {
            get => _EPFCategory;
            set { _EPFCategory = value; BroadcastRecalculateEvent(); }
        }
       
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

        private decimal _EPF_Employer;
        public decimal EPF_Employer
        {
            get => _EPF_Employer;
            set { _EPF_Employer = value; }
        }

        private decimal _EPF_Employee;
        public decimal EPF_Employee
        {
            get => _EPF_Employee;
            set { _EPF_Employee = value; OnRecalculateTotalPrice(); }
        }
        private decimal _Socso_Employer;
        public decimal Socso_Employer
        {
            get => _Socso_Employer;
            set { _Socso_Employer = value; }
        }
        private ESocsoCategory _SocsoCategory;
        public ESocsoCategory SocsoCategory
        {
            get => _SocsoCategory;
            set => _SocsoCategory = value;
        }
        private decimal _Socso_Employee;
        public decimal Socso_Employee
        {
            get => _Socso_Employee;
            set { _Socso_Employee = value; OnRecalculateTotalPrice(); }
        }
        private decimal _Deduction;

        public decimal Deduction
        {
            get => _Deduction;
            set { _Deduction = value; OnRecalculateTotalPrice(); }
        }
        private decimal _Gross_wages;
        public decimal Gross_wages
        {
            get => _Gross_wages;
            set { _Gross_wages = value; OnRecalculateTotalPrice(); BroadcastRecalculateEvent(); }
        }

   
        public decimal Total_wages { get; set; }

        private void BroadcastRecalculateEvent()
        {
            if (IsLoading)
            {
                return;
            }
            EPFRecalculateHandler?.Invoke(this, EventArgs.Empty);
        }

         private void RecalculateWages()
        {
            if (IsLoading)
            {
                return;
            }
                Daily_wages = DailyHours * DailyRate;
                OT_wages = OTHours * OTRate;
                Sunday_wages = SundayHours * SundayRate;
                Monthly_wages = MonthlyHours * MonthlyRate;
                Hourly_wages = HourlyHours * HourlyRate;
                OnRecalculateWages();
            }
        private void OnRecalculateTotalPrice()
        {
            if (IsLoading)
            {
                return;
            }

            Total_wages = Gross_wages - EPF_Employee- Socso_Employee - Deduction;
        }



            private void OnRecalculateWages()
            {
            if (IsLoading)
            {
                return;
            }

            Gross_wages = Daily_wages + OT_wages + Sunday_wages + Monthly_wages + Hourly_wages;
             
            }

        }

 
}

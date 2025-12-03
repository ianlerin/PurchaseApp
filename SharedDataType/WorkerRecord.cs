using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRecord
{
    public enum EWorkerStatus
    {
        Active,Inactive
    }
    public class WorkerRecord
    {
        public string? ID {  get; set; }
        public string? Name { get; set; }
        public string? Passport { get; set; }
        public decimal DailyRate { get; set; }
        public decimal OTRate { get; set; }

        public decimal SundayRate { get; set; }
        public decimal MonthlyRate { get; set; }

        public EWorkerStatus WorkerStatus { get; set; }
    }
}

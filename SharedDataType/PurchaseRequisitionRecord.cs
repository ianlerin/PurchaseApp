using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Numerics;


namespace PurchaseBlazorApp2.Components.Data
{
    public enum EDepartment
    {
        NotSpecified,
        OperationsDirector,
        EstateManager,
        ProcurementManager,
        ManagingDirector,
        AccountsSecurity,
        AccountsHQ,
        Admin
       
    }

    public enum ETask
    {
        PettyCash,
        PurchaseOrder,
        ContractsWithSupplier,
        FormalSupplierNotification,
        NDAWithSupplier,
        ReleaseInvoiceForPayment
    }

    public enum EPRStatus
    {
        Open,
        Close
    }



        public class POSubmitResponse
    {
        public bool bSuccess { get; set; }
        public List<string> IDs { get; set; } = new List<string>();
    }

    public class ApprovalInfo
    {
        public List<EDepartment> Departments { get; set; } = new List<EDepartment>();
        public string UserName { get; set; } = "";
        private bool _IsApproved;
        public bool IsApproved { get { return _IsApproved; } set { _IsApproved = value;
            } }
        public bool CanApprove(EDepartment UserDepartment)
        {
            if(Departments.Contains(UserDepartment))
            {
                return true;
            }
            return false;
        }

    }

    public class RequestItemInfo 
    {
        public string RequestItem { get; set; } = "";

        private decimal _UnitPrice;
        public decimal UnitPrice { get { return _UnitPrice; } set { _UnitPrice = value; OnRecalculateTotalPrice(); } }

        private decimal _Quantity;
        public decimal Quantity { get { return _Quantity; } set { _Quantity = value; OnRecalculateTotalPrice(); } }

        private decimal _TotalPrice;
        public decimal TotalPrice { get { return _TotalPrice; } set { _TotalPrice = value; } }

        private void OnRecalculateTotalPrice()
        {
            TotalPrice = Quantity * UnitPrice;
        }

    }


    public class ImageUploadInfo
    {
        public byte[] Data { get; set; }
        public string DataFormat {  get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class PurchaseOrderRecord
    {
        [Key]
        public string? PO_ID { get; set; }
        public string PR_ID { get; set; }
        public DateTime Date { get; set; }=DateTime.Now;
        public List<ApprovalInfo> ApprovalInfo { get; set; }= new List<ApprovalInfo> { };
        public EPRStatus PoStatus { get; set; }
        public void OnApprovalChanged()
        {
            foreach (var item in ApprovalInfo)
            {
                if (!item.IsApproved)
                {
                    PoStatus = EPRStatus.Open;
                    return;
                }
            }
            PoStatus = EPRStatus.Close;
        }
        public PurchaseOrderRecord()
        {
            ApprovalInfo POApprovalInfo = new ApprovalInfo();
            POApprovalInfo.Departments.Add(EDepartment.ProcurementManager);
            ApprovalInfo.Add(POApprovalInfo);
        }
    }

    public class PurchaseRequisitionRecord
    {
        [Key]
        public string? RequisitionNumber { get; set; } 
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string Requestor { get; set; }

        public string Purpose { get; set; }

        private List<RequestItemInfo> _ItemRequested = new List<RequestItemInfo>();
        public List<RequestItemInfo> ItemRequested { get { return _ItemRequested; } set { _ItemRequested = value; } }

        public EPRStatus prstatus { get; set; }
        public List<PurchaseBlazorApp2.Components.Data.ImageUploadInfo> SupportDocuments { get; set; }= new List<ImageUploadInfo>(); 
        public EDepartment Department { get; set; }

        private ETask _TaskType;
        public ETask TaskType { get { return _TaskType; } set { _TaskType = value; OnTaskTypeChanged(); } }


        private List<ApprovalInfo> _Approvals = new List<ApprovalInfo>();
        public List<ApprovalInfo> Approvals { get { return _Approvals; } set { _Approvals = value;} } 


        public void OnApprovalChanged()
        {
            foreach(var item in _Approvals)
            {
                if(!item.IsApproved)
                {
                    prstatus = EPRStatus.Open;
                    return;
                }
            }
            prstatus = EPRStatus.Close;
        }

        public PurchaseRequisitionRecord()
        {
            //OnTaskTypeChanged();
        }

        private decimal CalculateTotal()
        {
            decimal Count = 0;
            foreach (RequestItemInfo Info in ItemRequested)
            {
                Count += Info.TotalPrice;
            }
            return Count;
        }

        private void OnTaskTypeChanged()
        {
            return;
            Approvals.Clear();

            switch (TaskType)
            {
                case ETask.PettyCash:
                    {
                        ApprovalInfo approval1 = new ApprovalInfo();
                        approval1.Departments.Add(EDepartment.OperationsDirector);
                        ApprovalInfo approval2 = new ApprovalInfo();
                        approval2.Departments.Add(EDepartment.EstateManager);
                        Approvals.Add(approval1);
                        Approvals.Add(approval2);
                        return;
                    }

                case ETask.PurchaseOrder:
                    {
                        ApprovalInfo approval1 = new ApprovalInfo();
                        approval1.Departments.Add(EDepartment.OperationsDirector);
                        ApprovalInfo approval2 = new ApprovalInfo();
                        approval2.Departments.Add(EDepartment.EstateManager);
                        ApprovalInfo approval3 = new ApprovalInfo();

                        if (CalculateTotal() <= 10000)
                        {
                            approval3.Departments.Add(EDepartment.ProcurementManager);
                        }
                        else
                        {
                            approval3.Departments.Add(EDepartment.ProcurementManager);
                            ApprovalInfo approval4 = new ApprovalInfo();
                            approval4.Departments.Add(EDepartment.OperationsDirector); // Appears twice in logic
                            Approvals.Add(approval1);
                            Approvals.Add(approval2);
                            Approvals.Add(approval3);
                            Approvals.Add(approval4);
                            return;
                        }

                        Approvals.Add(approval1);
                        Approvals.Add(approval2);
                        Approvals.Add(approval3);
                        return;
                    }

                case ETask.ContractsWithSupplier:
                    {
                        ApprovalInfo approval1 = new ApprovalInfo();
                        approval1.Departments.Add(EDepartment.OperationsDirector);
                        ApprovalInfo approval2 = new ApprovalInfo();
                        approval2.Departments.Add(EDepartment.EstateManager);
                        ApprovalInfo approval3 = new ApprovalInfo();
                        approval3.Departments.Add(EDepartment.ProcurementManager);
                        ApprovalInfo approval4 = new ApprovalInfo();
                        approval4.Departments.Add(EDepartment.OperationsDirector); // Again repeated

                        Approvals.Add(approval1);
                        Approvals.Add(approval2);
                        Approvals.Add(approval3);
                        Approvals.Add(approval4);
                        return;
                    }

                case ETask.NDAWithSupplier:
                    {
                        ApprovalInfo approval1 = new ApprovalInfo();
                        approval1.Departments.Add(EDepartment.OperationsDirector);
                        approval1.Departments.Add(EDepartment.ManagingDirector);
                        ApprovalInfo approval3 = new ApprovalInfo();
                        approval3.Departments.Add(EDepartment.ProcurementManager);

                        Approvals.Add(approval1);
                        Approvals.Add(approval3);
                        return;
                    }

                case ETask.ReleaseInvoiceForPayment:
                    {
                        ApprovalInfo approval1 = new ApprovalInfo();
                        approval1.Departments.Add(EDepartment.ManagingDirector);
                        ApprovalInfo approval2 = new ApprovalInfo();
                        approval2.Departments.Add(EDepartment.AccountsHQ);
                        ApprovalInfo approval3 = new ApprovalInfo();
                        approval3.Departments.Add(EDepartment.AccountsSecurity);

                        Approvals.Add(approval1);
                        Approvals.Add(approval2);
                        Approvals.Add(approval3);
                        return;
                    }

                case ETask.FormalSupplierNotification:
                    {
                        ApprovalInfo approval1 = new ApprovalInfo();
                        approval1.Departments.Add(EDepartment.OperationsDirector);
                        ApprovalInfo approval2 = new ApprovalInfo();
                        approval2.Departments.Add(EDepartment.ProcurementManager);

                        Approvals.Add(approval1);
                        Approvals.Add(approval2);
                        return;
                    }

                default:
                    return;
            }
        }


    }
}

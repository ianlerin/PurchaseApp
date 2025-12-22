using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using System.Numerics;


namespace PurchaseBlazorApp2.Components.Data
{
    public enum EFilterPRType
    {
        Approval,
        CreatedBy

    }

    public enum EHRRole
    {
        Manager,
        None
    }
    public enum EDepartment
    {
        NotSpecified,
        OperationsDirector,
        EstateManager,
        ProcurementManager,
        ManagingDirector,
        AccountsSecurity,
        AccountsHQ,
        Admin,
        Finance
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

    public enum EPOStatus
    {
        GoodsNotReceived,
        GoodsReceived
    }
    public enum EPRSearchStatus
    {
        Full,Partial
    }
    public enum EPRStatus
    {
        PendingRequest,
        ApprovedRequests,
        PendingDelivery,
        ItemsReceived,
        Cancel,
        Close
    }

    public enum EApprovalStatus
    {
       Creation,
       PreApproval,
       PendingApproval,
       Approved,
       Rejected
    }

    public enum ESingleApprovalStatus
    {
        PendingAction,
        Rejected,
        Approved
    }
    public enum EPaymentStatus
    {
        PendingInvoice,
        PendingPayment,
        Paid
    }
    public class CredentialSubmitResponse
    {
        public bool bSuccess { get; set; }
        public UserName MyName { get; set; } = new UserName();
    }


    public class UserName
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }

        public UserName(string _Name, string _Password)
        {
            Name = _Name;
            Password = _Password;
        }
        public UserName()
        {

        }
        public EDepartment Role { get; set; }

        public EHRRole HRRole {  get; set; }
    }
    public class SupplierLookUpInfo
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string DisplayName => $"{ID}:{Name}";
    }

    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty;
       
        public string Base64Content{ get; set; } 
    }
    public class EmailRequest
    {
        public List<string> To { get; set; } = new();
        public List<string>? Cc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;
        public List<EmailAttachment> Attachments { get; set; } = new();
    }
    public class DeliveryDateUpdateRequest
    {
        public string PR_ID { get; set; }
        public DateTime DeliveryDate { get; set; }
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
        private ESingleApprovalStatus _ApproveStatus;
        public ESingleApprovalStatus ApproveStatus { get { return _ApproveStatus; } set {
                _ApproveStatus = value;
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

        public string Currency { get; set; } = "";


        private void OnRecalculateTotalPrice()
        {
            TotalPrice = Quantity * UnitPrice;
        }

    }

    public class ReceiveInfo
    {
        public string? po_id { get; set; }
        public List<ImageUploadInfo> SupportDocuments { get; set; } = new List<ImageUploadInfo>();
        public DateTime ReceiveDate { get; set; } = DateTime.Now;
    }


    public class InvoiceInfo
    {
        public string? po_id { get; set; }
        public List<ImageUploadInfo> SupportDocuments { get; set; } = new List<ImageUploadInfo>();
        private EPaymentStatus _PaymentStatus;
        public EPaymentStatus PaymentStatus
        {
            get { return _PaymentStatus; }
            set
            {
                if (_PaymentStatus == EPaymentStatus.PendingPayment && value == EPaymentStatus.Paid)
                {
                    bShouldSendEmail = true;
                }

                else if (_PaymentStatus == EPaymentStatus.PendingInvoice && value == EPaymentStatus.PendingPayment)
                {
                    bShouldSendEmail = true;
                }
                else
                {
                    bShouldSendEmail = false;
                }

                _PaymentStatus = value;
            }
        }
        public bool bShouldSendEmail = false;
    }

    public class ImageUploadInfo
    {
        public byte[] Data { get; set; }
        public string DataFormat {  get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
    }

    public class FinanceRecord
    {
        [Key]
        public string? PO_ID { get; set; }
        private EPaymentStatus _PaymentStatus = EPaymentStatus.PendingPayment;
        public EPaymentStatus PaymentStatus { get { return _PaymentStatus; } set { 
                if(_PaymentStatus==EPaymentStatus.PendingPayment&& value == EPaymentStatus.Paid)
                {
                    bShouldSendEmail = true;
                }

                else if (_PaymentStatus == EPaymentStatus.PendingInvoice && value == EPaymentStatus.PendingPayment)
                {
                    bShouldSendEmail = true;
                }
                else
                {
                    bShouldSendEmail = false;
                }

                    _PaymentStatus = value; } }

        public bool bShouldSendEmail = false;
        public Dictionary<decimal, FinanceRecordUpdate> FinanceRecordLists { get; set; } = new Dictionary<decimal, FinanceRecordUpdate>();
    }

    public class FinanceRecordUpdate
    {
        public List<ImageUploadInfo> SupportDocuments { get; set; } = new List<ImageUploadInfo>();
        public DateTime AddDate { get; set; } = DateTime.Now;
      
    }

        public class PurchaseOrderRecord
    {
        [Key]
        public string? PO_ID { get; set; }
        public string PR_ID { get; set; }
        public string CreatedBy { get; set; }

        //company info
        public string mycompanyname { get; set; }
        public string myaddress { get; set; }
        public string myemail { get; set; }
        public string tel { get; set; }

        public EPOStatus POStatus { get; set; }
        public bool bSentNotifiedEmail { get; set; }
        // supplier related info

        public string? suppliercompanyname { get; set; }
        public string? suppliercontactperson { get; set; }
        public string? suppliercontact { get; set; }
        public string? supplieremail { get; set; }

        //ship to detail
        public string? shiptocompanyname { get; set; }
        public string? warehouseaddress { get; set; }
        public string? receivingperson { get; set; }
        public string? shippingcontact { get; set; }
        public DateTime orderdate { get; set; } = DateTime.Now;

        //additional info
        public string? remark { get; set; }

        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }

        public decimal GetTotal()
        {
            return SubTotal + SubTotal * Tax;
        }

        //delivery info

        public DateTime DeliveryDate { get; set; } = DateTime.Now;
        public string? DeliveryMethod { get; set; }
        public string? PaymentMethod { get; set; }
        public ReceiveInfo ReceiveInfo { get; set; } = new ReceiveInfo();

        public InvoiceInfo InvoiceInfo { get; set; } = new InvoiceInfo();
        
        public PurchaseOrderRecord()
        {
          mycompanyname = "LCDA MSB PINEAPPLE SDN BHD 202201032786 (1478483-W)";
          myaddress = "166, Kampung Kovil, Lot 931 Mk17, 14000 Bukit Mertajam, Penang, Malaysia";
          myemail = "lcdamsbpineapple@gmail.com, nitsei1@hotmail.com";
          tel = "604-5308419/ 5370081";
          
        }
    }

    public class PurchaseRequisitionRecord
    {
        [Key]
        public string? RequisitionNumber { get; set; } 
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public DateTime DeliveryDate { get; set; } = DateTime.MinValue;
        public DateTime UpdateDate { get; set; } = DateTime.MinValue;
        public string Requestor { get; set; }
        public string? Rejectreason { get; set; }
        public string? Purpose { get; set; }

        public bool bSentReminder { get; set; }
        public string? po_id { get; set; } = "";

        private bool _burgent = false;
        public bool burgent { get { return _burgent; } set { _burgent = value; } }
        public string? Department { get; set; }

        private List<RequestItemInfo> _ItemRequested = new List<RequestItemInfo>();
        public List<RequestItemInfo> ItemRequested { get { return _ItemRequested; } set { _ItemRequested = value; } }

        private List<RequestItemInfo> _ApprovedItemRequested = new List<RequestItemInfo>();
        public List<RequestItemInfo> ApprovedItemRequested { get { return _ApprovedItemRequested; } set { _ApprovedItemRequested = value; } }

        public EPRStatus prstatus { get; set; }
        public EApprovalStatus approvalstatus { get; set; }
        public List<PurchaseBlazorApp2.Components.Data.ImageUploadInfo> SupportDocuments { get; set; }= new List<ImageUploadInfo>(); 
    
        private ETask _TaskType;
        public ETask TaskType { get { return _TaskType; } set { _TaskType = value; OnTaskTypeChanged(); } }

        public EPaymentStatus paymentstatus { get; set; }

        private List<ApprovalInfo> _Approvals = new List<ApprovalInfo>();
        public List<ApprovalInfo> Approvals { get { return _Approvals; } set { _Approvals = value; } }


        public void OnApprovalChanged()
        {
            if (_Approvals == null || _Approvals.Count == 0)
            {
                approvalstatus = EApprovalStatus.PreApproval;
                return;
            }

            // At least one approval exists → default to PendingApproval
            approvalstatus = EApprovalStatus.PendingApproval;

            bool allApproved = true;

            foreach (var item in _Approvals)
            {
                if (item.ApproveStatus == ESingleApprovalStatus.Rejected)
                {
                    approvalstatus = EApprovalStatus.Rejected;
                    return; // rejected overrides everything
                }

                if (item.ApproveStatus != ESingleApprovalStatus.Approved)
                {
                    allApproved = false; // found something not approved yet
                }
            }

            if (allApproved)
            {
                approvalstatus = EApprovalStatus.Approved;
            }
        }

        public void OnUpdatePRStatus()
        {
            // if already cancel dont ever change it
            if(prstatus==EPRStatus.Cancel)
            {
                return;
            }
            prstatus = EPRStatus.ApprovedRequests;
            if (Approvals.Count > 0)
            {
                foreach (var item in _Approvals)
                {
                    if (item.ApproveStatus != ESingleApprovalStatus.Approved)
                    {
                        prstatus = EPRStatus.PendingRequest;
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(po_id))
                {
                    prstatus = EPRStatus.PendingDelivery;
                }
            }
            else
            {
                prstatus = EPRStatus.PendingRequest;
            }

        }

        public PurchaseRequisitionRecord()
        {
            //OnApprovalChanged();
        }

        public decimal CalculateTotal()
        {
            decimal Count = 0;
            foreach (RequestItemInfo Info in ApprovedItemRequested)
            {
                Count += Info.TotalPrice;
            }
            return Count;
        }

        public HashSet<EDepartment> GetSelectedDepartments()
        {
            HashSet<EDepartment> ToReturn = new HashSet<EDepartment>();
            foreach (ApprovalInfo Approval in Approvals)
            {
                foreach(EDepartment Department in Approval.Departments)
                {
                    ToReturn.Add(Department);
                }
            
            }
            return ToReturn;
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

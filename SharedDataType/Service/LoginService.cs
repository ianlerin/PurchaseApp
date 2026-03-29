using PurchaseBlazorApp2.Components.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedDataType.Service
{
    public class CompanyState
    {
        public event Action? OnCompanyChanged;

        private CompanyInfo? _currentCompany;

        public CompanyInfo? CurrentCompany
        {
            get => _currentCompany;
            set
            {
                _currentCompany = value;
                OnCompanyChanged?.Invoke();
            }
        }

        public void Clear()
        {
            _currentCompany = null;
            OnCompanyChanged?.Invoke();
        }
    }
}

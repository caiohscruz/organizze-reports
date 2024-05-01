using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrganizzeReports.Console.ViewModels
{
    public class TransactionsSummaryViewModel
    {
        public string CategoryName { get; set; }
        public decimal TotalCurrentMonth { get; set; }
        public decimal TotalLastMonth { get; set; }
        public decimal MonthlyProportionLast3Months { get; set; }
        public decimal MonthlyProportionLast6Months { get; set; }
        public decimal MonthlyProportionLast12Months { get; set; }
        public decimal TotalLast3Months { get; set; }
        public decimal TotalLast6Months { get; set; }
        public decimal TotalLast12Months { get; set; }
    }
}

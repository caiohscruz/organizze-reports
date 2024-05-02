using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.DTOs;
using OrganizzeReports.Console.Services.ExcelService;
using OrganizzeReports.Console.ViewModels;
using System.Runtime.CompilerServices;

namespace OrganizzeReports.Console.Services
{
    public class ReportService
    {
        private OrganizzeAPIAdapter _apiAdapter;
        private ExcelService.ExcelService _excelService;

        /// <summary>
        /// Represents a collection of categories relevant for generating reports.
        /// </summary>
        private IEnumerable<CategoryDTO> _categories;

        /// <summary>
        /// Represents a collection of accounts relevant for generating reports.
        /// </summary>
        private IEnumerable<AccountDTO> _accounts;

        /// <summary>
        /// Represents a collection of credit cards relevant for generating reports.
        /// </summary>
        private IEnumerable<CreditCardDTO> _creditCards;

        /// <summary>
        /// Indicates whether the service has retrieved the necessary data for generating reports.
        /// </summary>
        private bool _isReady = false;

        /// <summary>
        /// Represents a collection of categories that are not relevant for generating reports.
        /// </summary>
        private readonly List<string> _categoriesToIgnore = new List<string>() { "Transferências", "Pagamento de fatura", "Comer Fora", "Giovanna Peral Salvadeo", "Cristiano" };

        /// <summary>
        ///  Represents a collection of distinct categories used for generating reports.
        /// </summary>
        private IEnumerable<string> _distinctCategories;

        public ReportService(OrganizzeAPIAdapter apiAdapter, ExcelService.ExcelService excelService)
        {
            _apiAdapter = apiAdapter;
            _excelService = excelService;
        }

        private async Task Init()
        {
            _categories = await GetCategoryWithEnrichedNames();
            _distinctCategories = _categories.Where(t => !_categoriesToIgnore.Contains(t.Name)).Select(t => t.Name).Distinct().OrderBy(c => c);
            _accounts = await _apiAdapter.GetAccounts();
            _creditCards = await _apiAdapter.GetCreditCards();
            _isReady = true;
        }

        public async Task GenerateCategoryReport()
        {
            if (!_isReady) await Init();

            // Retrieve transactions from Organizze API
            var transactionsDTOCurrentMonth = await _apiAdapter.GetTransactions();
            var transactionsDTO12MonthsAgo = await GetTransactionsFromPastMonths(12);
            var transactionsDTOLastMonth = transactionsDTO12MonthsAgo.Where(dto => dto.Date >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1));
            var transactionsDTO3MonthsAgo = transactionsDTO12MonthsAgo.Where(dto => dto.Date >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-3));
            var transactionsDTO6MonthsAgo = transactionsDTO12MonthsAgo.Where(dto => dto.Date >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-6));

            // Map transactions to view models
            var transactionsCurrentMonth = MapTransactionsViewModelFromDTO(transactionsDTOCurrentMonth);
            var transactionsLastMonth = MapTransactionsViewModelFromDTO(transactionsDTOLastMonth);
            var transactions3MonthsAgo = MapTransactionsViewModelFromDTO(transactionsDTO3MonthsAgo);
            var transactions6MonthsAgo = MapTransactionsViewModelFromDTO(transactionsDTO6MonthsAgo);
            var transactions12MonthsAgo = MapTransactionsViewModelFromDTO(transactionsDTO12MonthsAgo);

            // Generate summary view models
            var transactionsSummary = GetTransactionsSummaryViewModel(transactionsCurrentMonth, transactionsLastMonth, transactions3MonthsAgo, transactions6MonthsAgo, transactions12MonthsAgo);

            // Generate file path           
            string filePath = GetReportFilePath();

            var spreadSheets = new List<SpreadSheet>
            {
                new SpreadSheet { Name = "Resumo", Items = transactionsSummary, CurrencyColumns = Enumerable.Range(2, 8).ToList() },
                new SpreadSheet { Name = "Atual", Items = transactionsCurrentMonth, CurrencyColumns = new List<int>(){3} },
                new SpreadSheet { Name = "Anterior", Items = transactionsLastMonth, CurrencyColumns = new List<int>(){3} },
                new SpreadSheet { Name = "3Meses", Items = transactions3MonthsAgo, CurrencyColumns = new List<int>(){3} },
                new SpreadSheet { Name = "6Meses", Items = transactions6MonthsAgo, CurrencyColumns = new List<int>(){3} },
                new SpreadSheet { Name = "12Meses", Items = transactions12MonthsAgo, CurrencyColumns = new List<int>(){3} },
            };

            _excelService.GenerateExcelFile(filePath, spreadSheets);
        }

        private async Task<IEnumerable<CategoryDTO>> GetCategoryWithEnrichedNames()
        {
            var categories = await _apiAdapter.GetCategories();
            foreach (var category in categories)
            {
                if(category.ParentId != null)
                {
                    var parentCategory = categories.FirstOrDefault(c => c.Id == category.ParentId);
                    category.Name = $"[{parentCategory.Name}] {category.Name}";
                }
            }
            return categories;
        }

        private async Task<IEnumerable<TransactionDTO>> GetTransactionsFromPastMonths(int numberOfMonths)
        {
            var transactions = new List<TransactionDTO>();
            for (var i = 1; i <= numberOfMonths; i++)
            {
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-i);
                var endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-i+1).AddDays(-1);

                transactions.AddRange(await _apiAdapter.GetTransactions(startDate, endDate));

            }
            return transactions;
        }

        private IEnumerable<TransactionViewModel> MapTransactionsViewModelFromDTO(IEnumerable<TransactionDTO> dtos)
        {
            return dtos.Where(dto =>
            {   
                // filter out transactions with irrelevant categories
                var category = _categories.FirstOrDefault(c => c.Id == dto.CategoryId);
                return category != null && !_categoriesToIgnore.Contains(category.Name);
            }).Select(dto =>
            {
                var category = _categories.FirstOrDefault(c => c.Id == dto.CategoryId);
                var account = _accounts.FirstOrDefault(a => a.Id == dto.AccountId);
                var creditCard = _creditCards.FirstOrDefault(cc => cc.Id == dto.CreditCardId);

                return new TransactionViewModel
                {
                    Description = dto.Description,
                    Date = dto.Date,
                    Amount = dto.AmountCents != null ? Math.Round((decimal)dto.AmountCents / 100, 2) : 0M,
                    TotalInstallments = dto.TotalInstallments,
                    Installment = dto.Installment,
                    Recurring = dto.Recurring,
                    Account = account?.Name,
                    Category = category?.Name,
                    CreditCard = creditCard?.Name
                };
            })
            .Where(viewModel => viewModel != null);
        }

        private IEnumerable<TransactionsSummaryViewModel> GetTransactionsSummaryViewModel(IEnumerable<TransactionViewModel> currentMonth,
                                                                                IEnumerable<TransactionViewModel> lastMonth,
                                                                                IEnumerable<TransactionViewModel> threeMonths,
                                                                                IEnumerable<TransactionViewModel> sixMonths,
                                                                                IEnumerable<TransactionViewModel> twelveMonths)
        {
            return _distinctCategories.Select(category =>
            {
                var totalLast3Months = threeMonths.Where(t => t.Category == category).Sum(t => t.Amount);
                var totalLast6Months = sixMonths.Where(t => t.Category == category).Sum(t => t.Amount);
                var totalLast12Months = twelveMonths.Where(t => t.Category == category).Sum(t => t.Amount);

                return new TransactionsSummaryViewModel()
                {
                    CategoryName = category,
                    TotalCurrentMonth = currentMonth.Where(t => t.Category == category).Sum(t => t.Amount),
                    TotalLastMonth = lastMonth.Where(t => t.Category == category).Sum(t => t.Amount),
                    TotalLast3Months = totalLast3Months,
                    TotalLast6Months = totalLast6Months,
                    TotalLast12Months = totalLast12Months,
                    MonthlyProportionLast3Months = totalLast3Months / 3,
                    MonthlyProportionLast6Months = totalLast6Months / 6,
                    MonthlyProportionLast12Months = totalLast12Months / 12

                };
            });
        }
        private string GetReportFilePath()
        {
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"finantial_report_{timestamp}.xlsx";
            return Path.Combine(downloadsPath, "Downloads", fileName);
        }
    }

}


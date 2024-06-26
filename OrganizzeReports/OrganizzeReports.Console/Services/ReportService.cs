﻿using Microsoft.Extensions.Configuration;
using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.DTOs;
using OrganizzeReports.Console.Services.ExcelService;
using OrganizzeReports.Console.ViewModels;

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
        private async Task LoadCategories()
        {
            var categories = await _apiAdapter.GetCategories();
            categories = GetCategoryWithEnrichedNames(categories);
            _categories = FilterOutCategoriesToIgnore(categories);
        }

        /// <summary>
        /// Represents a collection of accounts relevant for generating reports.
        /// </summary>
        private IEnumerable<AccountDTO> _accounts;
        private async Task LoadAccounts()
        {
            _accounts = await _apiAdapter.GetAccounts();
        }

        /// <summary>
        /// Represents a collection of credit cards relevant for generating reports.
        /// </summary>
        private IEnumerable<CreditCardDTO> _creditCards;
        private async Task LoadCreditCards()
        {
            _creditCards = await _apiAdapter.GetCreditCards();
        }

        /// <summary>
        /// Represents a collection of invoices relevant for generating reports.
        /// </summary>
        private IEnumerable<InvoiceDTO> _invoices;
        private async Task LoadInvoices()
        {
            if (!_creditCards.Any())
            {
                await LoadCreditCards();
            }
            else
            {
                foreach (var creditCard in _creditCards)
                {
                    var invoices = await _apiAdapter.GetInvoices(creditCard.Id);
                    _invoices = _invoices.Concat(invoices);
                }
            }

        }

        /// <summary>
        /// Represents a collection of categories that are not relevant for generating reports.
        /// </summary>
        private readonly List<string> _categoriesToIgnore;

        public ReportService(OrganizzeAPIAdapter apiAdapter, ExcelService.ExcelService excelService, IConfiguration configuration)
        {
            _apiAdapter = apiAdapter;
            _excelService = excelService;
            _categoriesToIgnore = configuration.GetSection("ReportSettings:CategoriesToIgnore").Get<List<string>>();
        }

        /// <summary>
        /// The report focuses on analyzing transactions clustered by their respective categories.
        /// Spreadsheets will be generated to present the transactions segregated by periods and a spreadsheet to 
        /// present the amounts by category and period.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateCategoryReport()
        {
            if (!_categories.Any()) await LoadCategories();

            // Retrieve transactions from Organizze API
            var transactionsDTOCurrentMonth = await _apiAdapter.GetTransactions();
            var transactionsDTO12MonthsAgo = await GetTransactionsFromPastMonths(12);
            var transactionsDTONext12Months = await GetTransactionsFromNextMonths(12);

            // Map transactions to view models
            var transactionsCurrentMonth = MapTransactionsViewModelFromDTO(transactionsDTOCurrentMonth);
            var transactions12MonthsAgo = MapTransactionsViewModelFromDTO(transactionsDTO12MonthsAgo);
            var transactionsNext12Months = MapTransactionsViewModelFromDTO(transactionsDTONext12Months);

            // Filter out transactions of ignored categories
            transactionsCurrentMonth = FilterOutTransactionsWithoutCategoryLinked(transactionsCurrentMonth);
            transactions12MonthsAgo = FilterOutTransactionsWithoutCategoryLinked(transactions12MonthsAgo);
            transactionsNext12Months = FilterOutTransactionsWithoutCategoryLinked(transactionsNext12Months);

            // Segregate transactions by period
            var transactionsLastMonth = transactions12MonthsAgo.Where(dto => dto.Date >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1));
            var transactions3MonthsAgo = transactions12MonthsAgo.Where(dto => dto.Date >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-3));
            var transactions6MonthsAgo = transactions12MonthsAgo.Where(dto => dto.Date >= new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-6));

            // Generate summary view models
            var transactionsSummary = GetTransactionsSummaryViewModel(transactionsCurrentMonth, transactionsLastMonth, transactions3MonthsAgo, transactions6MonthsAgo, transactions12MonthsAgo);

            // Generate future estimation view models
            var futureEstimations = GetFutureEstimationViewModels(transactionsNext12Months);

            // Generate file path
            string filePath = GetReportFilePath();

            var spreadSheets = new List<SpreadSheet>
                {
                    new SpreadSheet { Name = "Resumo", Items = transactionsSummary, CurrencyColumns = Enumerable.Range(2, 8).ToList() },
                    new SpreadSheet { Name = "Estimativa", Items = futureEstimations, CurrencyColumns = Enumerable.Range(1, 12).ToList() },
                    new SpreadSheet { Name = "Atual", Items = transactionsCurrentMonth, CurrencyColumns = new List<int>(){3} },
                    new SpreadSheet { Name = "Anterior", Items = transactionsLastMonth, CurrencyColumns = new List<int>(){3} },
                    new SpreadSheet { Name = "3Meses", Items = transactions3MonthsAgo, CurrencyColumns = new List<int>(){3} },
                    new SpreadSheet { Name = "6Meses", Items = transactions6MonthsAgo, CurrencyColumns = new List<int>(){3} },
                    new SpreadSheet { Name = "12Meses", Items = transactions12MonthsAgo, CurrencyColumns = new List<int>(){3} },
                };

            _excelService.GenerateExcelFile(filePath, spreadSheets);
        }

        #region Data Transformation
        /// <summary>
        /// Retrieves categories with enriched names, including the parent category name for nested categories.
        /// </summary>
        /// <returns>A collection of CategoryDTO objects with enriched names.</returns>
        private IEnumerable<CategoryDTO> GetCategoryWithEnrichedNames(IEnumerable<CategoryDTO> categories)
        {
            foreach (var category in categories)
            {
                if (category.ParentId != null)
                {
                    var parentCategory = categories.FirstOrDefault(c => c.Id == category.ParentId);
                    category.Name = $"{parentCategory.Name} > {category.Name}";
                }
            }
            return categories;
        }

        /// <summary>
        /// Retrieves transactions from the past specified number of months.
        /// </summary>
        /// <param name="numberOfMonths">The number of months to retrieve transactions from.</param>
        /// <returns>A collection of TransactionDTO objects representing the retrieved transactions.</returns>
        private async Task<IEnumerable<TransactionDTO>> GetTransactionsFromPastMonths(int numberOfMonths)
        {
            var transactions = new List<TransactionDTO>();
            for (var i = 1; i <= numberOfMonths; i++)
            {
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-i);
                var endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-i + 1).AddDays(-1);

                transactions.AddRange(await _apiAdapter.GetTransactions(startDate, endDate));
            }
            return transactions;
        }

        /// <summary>
        /// Retrieves transactions from the next specified number of months.
        /// </summary>
        /// <param name="numberOfMonths">The number of months to retrieve transactions from.</param>
        /// <returns>A collection of TransactionDTO objects representing the retrieved transactions.</returns>
        private async Task<IEnumerable<TransactionDTO>> GetTransactionsFromNextMonths(int numberOfMonths)
        {
            var transactions = new List<TransactionDTO>();
            for (var i = 1; i <= numberOfMonths; i++)
            {
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(i - 1);
                var endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(i).AddDays(-1);

                transactions.AddRange(await _apiAdapter.GetTransactions(startDate, endDate));
            }
            return transactions;
        }

        /// <summary>
        /// Maps the transaction data from DTOs to view models.
        /// </summary>
        /// <param name="dtos">The collection of TransactionDTO objects.</param>
        /// <returns>The collection of TransactionViewModel objects.</returns>
        private IEnumerable<TransactionViewModel> MapTransactionsViewModelFromDTO(IEnumerable<TransactionDTO> dtos)
        {
            return dtos.Select(dto =>
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
            });
        }

        /// <summary>
        /// Filters out transactions of ignored categories from the given collection of transactions.
        /// </summary>
        /// <param name="transactions">The collection of transactions to filter.</param>
        /// <returns>The filtered collection of transactions.</returns>
        private IEnumerable<TransactionViewModel> FilterOutTransactionsWithoutCategoryLinked(IEnumerable<TransactionViewModel> transactions)
        {
            return transactions.Where(transaction =>
            {
                return transaction.Category != null;
            });
        }

        /// <summary>
        /// Filters out categories that are not relevant for generating reports.
        /// </summary>
        /// <param name="categories"></param>
        /// <returns></returns>
        private IEnumerable<CategoryDTO> FilterOutCategoriesToIgnore(IEnumerable<CategoryDTO> categories)
        {
            var parentCategories = categories.Select(c => c.ParentId).Distinct();
            return categories.Where(category => !category.Archived && !parentCategories.Contains(category.Id) && !_categoriesToIgnore.Contains(category.Name));
        }
        #endregion

        /// <summary>
        /// Generates a collection of TransactionsSummaryViewModel objects based on the given collections of transactions.
        /// </summary>
        /// <param name="currentMonth">Transactions of the current month</param>
        /// <param name="lastMonth">Transactions of the last month</param>
        /// <param name="threeMonths">Transactions of the last three months</param>
        /// <param name="sixMonths">Transactions of the last six months</param>
        /// <param name="twelveMonths">Transactions of the last twelve months</param>
        /// <returns>A collection of TransactionsSummaryViewModel objects</returns>
        private IEnumerable<TransactionsSummaryViewModel> GetTransactionsSummaryViewModel(IEnumerable<TransactionViewModel> currentMonth,
                                                                                IEnumerable<TransactionViewModel> lastMonth,
                                                                                IEnumerable<TransactionViewModel> threeMonths,
                                                                                IEnumerable<TransactionViewModel> sixMonths,
                                                                                IEnumerable<TransactionViewModel> twelveMonths)
        {
            var distinctCategories = _categories.Select(t => t.Name).Distinct().OrderBy(c => c);

            var data = distinctCategories.Select(category =>
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

            var sumRow = data.Aggregate(new TransactionsSummaryViewModel(), (acc, item) =>
            {
                acc.CategoryName = "----TOTAL----";
                acc.TotalCurrentMonth += item.TotalCurrentMonth;
                acc.TotalLastMonth += item.TotalLastMonth;
                acc.TotalLast3Months += item.TotalLast3Months;
                acc.TotalLast6Months += item.TotalLast6Months;
                acc.TotalLast12Months += item.TotalLast12Months;
                acc.MonthlyProportionLast3Months += item.MonthlyProportionLast3Months;
                acc.MonthlyProportionLast6Months += item.MonthlyProportionLast6Months;
                acc.MonthlyProportionLast12Months += item.MonthlyProportionLast12Months;
                return acc;
            });

            return data.Append(sumRow);
        }

        /// <summary>
        /// Generates a collection of FutureEstimationViewModel objects based on the given collection of transactions.
        /// </summary>
        /// <param name="transactions">The collection of TransactionViewModel objects.</param>
        /// <returns>A collection of FutureEstimationViewModel objects.</returns>
        private IEnumerable<FutureEstimationViewModel> GetFutureEstimationViewModels(IEnumerable<TransactionViewModel> transactions)
        {
            var distinctCategories = _categories.Select(t => t.Name).Distinct().OrderBy(c => c);

            var nextTransactionsSegregatedByMonth = new List<IEnumerable<TransactionViewModel>>();

            for (var i = 1; i <= 12; i++)
            {
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(i - 1);
                var endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(i).AddDays(-1);

                var transactionsByMonth = transactions.Where(dto => dto.Date >= startDate && dto.Date <= endDate);

                nextTransactionsSegregatedByMonth.Add(transactionsByMonth);
        }

            var data = distinctCategories.Select(category =>
            {
                return new FutureEstimationViewModel()
                {
                    CategoryName = category,
                    Month1 = nextTransactionsSegregatedByMonth[0].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month2 = nextTransactionsSegregatedByMonth[1].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month3 = nextTransactionsSegregatedByMonth[2].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month4 = nextTransactionsSegregatedByMonth[3].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month5 = nextTransactionsSegregatedByMonth[4].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month6 = nextTransactionsSegregatedByMonth[5].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month7 = nextTransactionsSegregatedByMonth[6].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month8 = nextTransactionsSegregatedByMonth[7].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month9 = nextTransactionsSegregatedByMonth[8].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month10 = nextTransactionsSegregatedByMonth[9].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month11 = nextTransactionsSegregatedByMonth[10].Where(t => t.Category == category).Sum(t => t.Amount),
                    Month12 = nextTransactionsSegregatedByMonth[11].Where(t => t.Category == category).Sum(t => t.Amount)
                };
            });

            const decimal contentmentPlan = 2500;
            decimal actualMonthPlan = contentmentPlan + nextTransactionsSegregatedByMonth[0].Where(t => t.Recurring == false && t.Installment == 1 && t.Amount < 0).Sum(t => t.Amount);
            actualMonthPlan = actualMonthPlan < 0 ? 0 : actualMonthPlan;

            data = data.Append(new FutureEstimationViewModel()
            {
                CategoryName = "----Plano de Contenção----",
                Month1 = -actualMonthPlan,
                Month2 = -contentmentPlan,
                Month3 = -contentmentPlan,
                Month4 = -contentmentPlan,
                Month5 = -contentmentPlan,
                Month6 = -contentmentPlan,
                Month7 = -contentmentPlan,
                Month8 = -contentmentPlan,
                Month9 = -contentmentPlan,
                Month10 = -contentmentPlan,
                Month11 = -contentmentPlan,
                Month12 = -contentmentPlan,
            });

            var sumRow = data.Aggregate(new FutureEstimationViewModel(), (acc, item) =>
            {
                acc.CategoryName = "----TOTAL----";
                acc.Month1 += item.Month1;
                acc.Month2 += item.Month2;
                acc.Month3 += item.Month3;
                acc.Month4 += item.Month4;
                acc.Month5 += item.Month5;
                acc.Month6 += item.Month6;
                acc.Month7 += item.Month7;
                acc.Month8 += item.Month8;
                acc.Month9 += item.Month9;
                acc.Month10 += item.Month10;
                acc.Month11 += item.Month11;
                acc.Month12 += item.Month12;
                return acc;
            });

            return data.Append(sumRow);
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


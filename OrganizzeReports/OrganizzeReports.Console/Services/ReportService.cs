using ClosedXML.Excel;
using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.DTOs;
using OrganizzeReports.Console.ViewModels;
using System.Transactions;

namespace OrganizzeReports.Console.Services
{
    public class ReportService
    {
        private OrganizzeAPIAdapter _apiAdapter;

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
        private readonly IEnumerable<string> _categoriesToIgnore = new List<string>() { "Transferências" };

        /// <summary>
        ///  Represents a collection of distinct categories used for generating reports.
        /// </summary>
        private IEnumerable<string> _distinctCategories;

        public ReportService(OrganizzeAPIAdapter apiAdapter) { 
            _apiAdapter = apiAdapter;
        }

        private async Task Init()
        {
            _categories = await _apiAdapter.GetCategories();
            _distinctCategories = _categories.Where(t => t.Archived == false && !_categoriesToIgnore.Contains(t.Name)).Select(t => t.Name).Distinct().OrderBy(c => c);
            _accounts = await _apiAdapter.GetAccounts();
            _creditCards = await _apiAdapter.GetCreditCards();
            _isReady = true;
        }

        public async Task GenerateExcel()
        {
            if(!_isReady) await Init();

            // Obter as transações
            var transactionsDTOActualMonth = await _apiAdapter.GetTransactions();
            var transactionsDTOLastMonth = await GetTransactionsForMonthsAgo(1);
            var transactionsDTO3MonthsAgo = await GetTransactionsForMonthsAgo(3);
            var transactionsDTO6MonthsAgo = await GetTransactionsForMonthsAgo(6);
            var transactionsDTO12MonthsAgo = await GetTransactionsForMonthsAgo(12);

            // Mapear IDs para nomes ou detalhes correspondentes
            var transactionsActualMonth = MapTransactions(transactionsDTOActualMonth);
            var transactionsLastMonth = MapTransactions(transactionsDTOLastMonth);
            var transactions3MonthsAgo = MapTransactions(transactionsDTO3MonthsAgo);
            var transactions6MonthsAgo = MapTransactions(transactionsDTO6MonthsAgo);
            var transactions12MonthsAgo = MapTransactions(transactionsDTO12MonthsAgo);

            // Nome e Caminho do arquivo
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"finantial_report_{timestamp}.xlsx";
            string filePath = Path.Combine(downloadsPath, "Downloads", fileName);
         

            using (var workbook = new XLWorkbook())
            {
                AddTransactionsSheet(workbook, transactionsActualMonth, "TransaçõesMêsAtual");
                AddSummaryTransactionsSheet(workbook, transactionsActualMonth, "CompiladoMêsAtual");
                AddTransactionsSheet(workbook, transactionsLastMonth, "TransaçõesMêsPassado");
                AddSummaryTransactionsSheet(workbook, transactionsLastMonth, "CompiladoMêsPassado");
                AddTransactionsSheet(workbook, transactions3MonthsAgo, "Transações3MesesAnteriores");
                AddSummaryTransactionsSheet(workbook, transactions3MonthsAgo, "Compilado3MesesAnteriores", 3);
                AddTransactionsSheet(workbook, transactions6MonthsAgo, "Transações6MesesAnteriores");
                AddSummaryTransactionsSheet(workbook, transactions6MonthsAgo, "Compilado6MesesAnteriores", 6);
                AddTransactionsSheet(workbook, transactions12MonthsAgo, "Transações12MesesAnteriores");
                AddSummaryTransactionsSheet(workbook, transactions12MonthsAgo, "Compilado12MesesAnteriores", 12);

                workbook.SaveAs(filePath);
            }
        }

        private void AddSummaryTransactionsSheet(XLWorkbook workbook, IEnumerable<TransactionViewModel> transactions, string sheetName, int numMonths = 1)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);

            worksheet.Cell(1, 1).Value = "Category";
            worksheet.Cell(1, 2).Value = "Total";

            if(numMonths > 1) worksheet.Cell(1, 3).Value = "TotalPerMonth";

            int row = 1;
            foreach (var category in _distinctCategories)
            {
                worksheet.Cell(++row, 1).Value = category;
            }

            row = 1;
            foreach (var category in _distinctCategories)
            {
                var sumAmount = transactions.Where(t => t.Category == category).Sum(t => t.Amount);
                worksheet.Cell(++row, 2).Value = sumAmount;

                if (numMonths > 1) worksheet.Cell(row, 3).Value = sumAmount / numMonths; 
            }

            var amountColumn = worksheet.Column("B");
            amountColumn.Style.NumberFormat.Format = "R$ #,##0.00";

            if (numMonths > 1)
            {
                var amountPerMonthColumn = worksheet.Column("C");
                amountPerMonthColumn.Style.NumberFormat.Format = "R$ #,##0.00";
            }

            worksheet.Columns().AdjustToContents();
        }

        private void AddTransactionsSheet(XLWorkbook workbook, IEnumerable<TransactionViewModel> transactions, string sheetName)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).InsertTable(transactions);
            // Formata as células da coluna Amount para moeda BRL
            var amountColumn = worksheet.Column("C");
            amountColumn.Style.NumberFormat.Format = "R$ #,##0.00";

            worksheet.Columns().AdjustToContents();
        }

        private async Task<IEnumerable<TransactionDTO>> GetTransactionsForMonthsAgo(int numberOfMonths)
        {
            var startDate = DateTime.Now.AddMonths(-numberOfMonths).AddDays(1 - DateTime.Now.Day);
            var endDate = DateTime.Now.AddDays(-DateTime.Now.Day);
            return await _apiAdapter.GetTransactions(startDate, endDate);
        }

        private IEnumerable<TransactionViewModel> MapTransactions (IEnumerable<TransactionDTO> transactions)
        {
            return transactions.Select(transaction =>
            {
                var account = _accounts.FirstOrDefault(a => a.Id == transaction.AccountId);
                var category = _categories.FirstOrDefault(c => c.Id == transaction.CategoryId);
                var creditCard = _creditCards.FirstOrDefault(cc => cc.Id == transaction.CreditCardId);

                return new TransactionViewModel
                {
                    Description = transaction.Description,
                    Date = transaction.Date,
                    Amount = transaction.AmountCents != null ? Math.Round((decimal)transaction.AmountCents / 100, 2) : 0M,
                    TotalInstallments = transaction.TotalInstallments,
                    Installment = transaction.Installment,
                    Recurring = transaction.Recurring,
                    Account = account?.Name,
                    Category = category?.Name,
                    CreditCard = creditCard?.Name
                };
            });
        }
    }

}


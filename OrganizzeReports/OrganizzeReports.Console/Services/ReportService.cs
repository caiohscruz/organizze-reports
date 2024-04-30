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

        private IEnumerable<CategoryDTO> _categories;
        private IEnumerable<string> _distinctCategories;
        private IEnumerable<AccountDTO> _accounts;
        private IEnumerable<CreditCardDTO> _creditCards;

        private bool _isReady = false;

        public ReportService(OrganizzeAPIAdapter apiAdapter) { 
            _apiAdapter = apiAdapter;
        }

        private async Task Init()
        {
            _categories = await _apiAdapter.GetCategories();
            _distinctCategories = _categories.Where(t => t.Archived == false).Select(t => t.Name).Distinct().OrderBy(c => c);
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
                AddSummaryTransactionsSheet(workbook, transactions3MonthsAgo, "Compilado3MesesAnteriores");
                AddTransactionsSheet(workbook, transactions6MonthsAgo, "Transações6MesesAnteriores");
                AddSummaryTransactionsSheet(workbook, transactions6MonthsAgo, "Compilado6MesesAnteriores");
                AddTransactionsSheet(workbook, transactions12MonthsAgo, "Transações12MesesAnteriores");
                AddSummaryTransactionsSheet(workbook, transactions12MonthsAgo, "Compilado12MesesAnteriores");

                workbook.SaveAs(filePath);
            }
        }

        private void AddSummaryTransactionsSheet(XLWorkbook workbook, IEnumerable<TransactionViewModel> transactions, string sheetName)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);

            int row = 1;
            foreach (var category in _distinctCategories)
            {
                worksheet.Cell(row++, 1).Value = category;
            }

            row = 1;
            foreach (var category in _distinctCategories)
            {
                var sumAmount = transactions.Where(t => t.Category == category).Sum(t => t.Amount);
                worksheet.Cell(row++, 2).Value = sumAmount;
            }

            var amountColumn = worksheet.Column("B");
            amountColumn.Style.NumberFormat.Format = "R$ #,##0.00";
        }

        private void AddTransactionsSheet(XLWorkbook workbook, IEnumerable<TransactionViewModel> transactions, string sheetName)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).InsertTable(transactions);
            // Formata as células da coluna Amount para moeda BRL
            var amountColumn = worksheet.Column("C");
            amountColumn.Style.NumberFormat.Format = "R$ #,##0.00";
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


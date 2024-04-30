using ClosedXML.Excel;
using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.DTOs;
using OrganizzeReports.Console.ViewModels;

namespace OrganizzeReports.Console.Services
{
    public class ReportService
    {
        private OrganizzeAPIAdapter _apiAdapter;

        private IEnumerable<CategoryDTO> _categories;
        private IEnumerable<AccountDTO> _accounts;
        private IEnumerable<CreditCardDTO> _creditCards;

        private bool _isReady = false;

        public ReportService(OrganizzeAPIAdapter apiAdapter) { 
            _apiAdapter = apiAdapter;
        }

        private async Task Init()
        {
            _categories = await _apiAdapter.GetCategories();
            _accounts = await _apiAdapter.GetAccounts();
            _creditCards = await _apiAdapter.GetCreditCards();
            _isReady = true;
        }

        public async Task GetTransactions()
        {
            if(!_isReady) await Init();

            // Obter as transações
            var transactionDTOs = await _apiAdapter.GetTransactions();

            // Mapear IDs para nomes ou detalhes correspondentes
            var transactions = transactionDTOs.Select(transaction =>
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

            GenerateExcel(transactions);
        }       

        private void GenerateExcel(IEnumerable<TransactionViewModel> transactions)
        {
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"finantial_report_{timestamp}.xlsx";
            string filePath = Path.Combine(downloadsPath, "Downloads", fileName);

            using (var workbook = new XLWorkbook())
            {
                // Aba Transações do Mês Atual
                var worksheetTransactions = workbook.Worksheets.Add("TransaçõesMesAtual");
                worksheetTransactions.Cell(1, 1).InsertTable(transactions);

                // Aba Compilado do Mês Atual
                var worksheetSummary = workbook.Worksheets.Add("CompiladoMesAtual");

                // Obter categorias distintas
                var distinctCategories = transactions.Select(t => t.Category).Distinct().OrderBy(c => c);

                // Escrever categorias distintas na primeira coluna
                int row = 1;
                foreach (var category in distinctCategories)
                {
                    worksheetSummary.Cell(row++, 1).Value = category;
                }

                // Calcular e escrever soma dos amounts para cada categoria
                row = 1;
                foreach (var category in distinctCategories)
                {
                    var sumAmount = transactions.Where(t => t.Category == category).Sum(t => t.Amount);
                    worksheetSummary.Cell(row++, 2).Value = sumAmount;
                }

                workbook.SaveAs(filePath);
            }
        }
    }

}


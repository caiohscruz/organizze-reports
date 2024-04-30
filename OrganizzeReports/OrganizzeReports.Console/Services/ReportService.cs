using CsvHelper;
using CsvHelper.Configuration;
using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.DTOs;
using OrganizzeReports.Console.ViewModels;
using System.Globalization;

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

            GenerateCsv(transactions);
        }

        private void GenerateCsv(IEnumerable<TransactionViewModel> transactions)
        {
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string filePath = Path.Combine(downloadsPath, "Downloads", "transactions.csv");

            using (var writer = new StreamWriter(filePath)) 
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(transactions);
            }
        }

    }
}

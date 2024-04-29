using OrganizzeReports.Console.Adapters;
using OrganizzeReports.Console.DTOs;
using OrganizzeReports.Console.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<TransactionViewModel>> GetTransactions()
        {
            if(!_isReady) await Init();

            // Obter as transações
            var transactionDTOs = await _apiAdapter.GetTransactions();

            // Mapear IDs para nomes ou detalhes correspondentes
            var transactionsWithDetails = transactionDTOs.Select(transaction =>
            {
                var account = _accounts.FirstOrDefault(a => a.Id == transaction.AccountId);
                var category = _categories.FirstOrDefault(c => c.Id == transaction.CategoryId);
                var creditCard = _creditCards.FirstOrDefault(cc => cc.Id == transaction.CreditCardId);

                return new TransactionViewModel
                {
                    Description = transaction.Description,
                    Date = transaction.Date,
                    Amount = transaction.AmountCents != null ? Math.Round((decimal)transaction.AmountCents / 100, 2) : 0M,
                    Installment = transaction.Installment,
                    Recurring = transaction.Recurring,
                    Account = account.Name,
                    Category = category.Name,
                    CreditCard = creditCard.Name
                };
            });

            return transactionsWithDetails;
        }

    }
}

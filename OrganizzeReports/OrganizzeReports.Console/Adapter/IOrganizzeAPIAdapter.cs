using OrganizzeReports.Console.DTOs;

namespace OrganizzeReports.Console.Adapter
{
    public interface IOrganizzeAPIAdapter
    {
        Task<IEnumerable<AccountDTO>> GetAccounts();
        Task<IEnumerable<CategoryDTO>> GetCategories();
        Task<IEnumerable<CreditCardDTO>> GetCreditCards();
        Task<IEnumerable<TransactionDTO>> GetTransactions();
    }
}
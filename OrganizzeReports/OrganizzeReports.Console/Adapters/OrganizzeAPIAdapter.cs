using OrganizzeReports.Console.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OrganizzeReports.Console.Adapters
{
    public class OrganizzeAPIAdapter
    {
        #region Endpoints
        private enum Endpoint
        {
            CreditCards,
            Categories,
            Accounts,
            Transactions
        }
        private string GetEndpointUrl(Endpoint endpoint)
        {
            string endpointPath = endpoint switch
            {
                Endpoint.CreditCards => "credit_cards",
                Endpoint.Categories => "categories",
                Endpoint.Accounts => "accounts",
                Endpoint.Transactions => "transactions",
                _ => throw new ArgumentException("Endpoint desconhecido"),
            };

            return $"{_baseUrl}/{endpointPath}";

        }
        #endregion

        private readonly string _baseUrl = "https://api.organizze.com.br/rest/v2";
        private readonly HttpClient _httpClient;
        public OrganizzeAPIAdapter(string name, string email, string apiKey)
        {
            HttpClient client = new HttpClient();
            //client.BaseAddress = new Uri(_baseUrl);
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{email}:{apiKey}")));
            client.DefaultRequestHeaders.Add("User-Agent", $"{name} ({email})");
            _httpClient = client;
        }

        #region Requests
        /// <summary>
        /// Retrieves categories
        /// </summary>
        /// <returns>Returns all categories</returns>
        public async Task<IEnumerable<CategoryDTO>> GetCategories()
        {
            return await GetEndpointData<CategoryDTO>(Endpoint.Categories);
        }

        /// <summary>
        /// Retrieves transactions of the current month
        /// </summary>
        /// <returns>Returns all transactions from the current month</returns>
        public async Task<IEnumerable<TransactionDTO>> GetTransactions()
        {
            return await GetEndpointData<TransactionDTO>(Endpoint.Transactions);
        }

        /// <summary>
        /// Retrieves transactions within a specified period.
        /// </summary>
        /// <param name="startDate">The start date of the period.</param>
        /// <param name="endDate">The end date of the period.</param>
        /// <returns>Returns all transactions that occurred between the start and end dates, inclusive.</returns>
        public async Task<IEnumerable<TransactionDTO>> GetTransactions(DateTime startDate, DateTime endDate)
        {
            var queryString = $"?start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}";
            return await GetEndpointData<TransactionDTO>(Endpoint.Transactions, queryString);
        }

        /// <summary>
        /// Retrieves accounts
        /// </summary>
        /// <returns>Returns all accounts</returns>
        public async Task<IEnumerable<AccountDTO>> GetAccounts()
        {
            return await GetEndpointData<AccountDTO>(Endpoint.Accounts);
        }

        /// <summary>
        /// Retrieves credit cards
        /// </summary>
        /// <returns>Returns all credit cards</returns>
        public async Task<IEnumerable<CreditCardDTO>> GetCreditCards()
        {
            return await GetEndpointData<CreditCardDTO>(Endpoint.CreditCards);
        }
        #endregion

        #region Utils
        private async Task<IEnumerable<T>> GetEndpointData<T>(Endpoint endpoint, string queryString = null)
        {
            string endpointUrl = GetEndpointUrl(endpoint);

            if(queryString != null) endpointUrl += queryString;

            HttpResponseMessage response = await _httpClient.GetAsync(endpointUrl);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<T>>(json);
            }
            else
            {
                System.Console.WriteLine($"Erro na requisição para {endpoint}: {response.StatusCode}");
                return default;
            }
        }
        #endregion
    }
}

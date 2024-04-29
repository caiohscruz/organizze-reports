using OrganizzeReports.Console.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OrganizzeReports.Console.Adapter
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
        public async Task<IEnumerable<CategoryDTO>> GetCategories()
        {
            return await GetEndpointData<CategoryDTO>(Endpoint.Categories);
        }

        public async Task<IEnumerable<TransactionDTO>> GetTransactions()
        {
            return await GetEndpointData<TransactionDTO>(Endpoint.Transactions);
        }

        public async Task<IEnumerable<AccountDTO>> GetAccounts()
        {
            return await GetEndpointData<AccountDTO>(Endpoint.Accounts);
        }

        public async Task<IEnumerable<CreditCardDTO>> GetCreditCards()
        {
            return await GetEndpointData<CreditCardDTO>(Endpoint.CreditCards);
        }
        #endregion

        #region Utils
        private async Task<IEnumerable<T>> GetEndpointData<T>(Endpoint endpoint)
        {
            string endpointUrl = GetEndpointUrl(endpoint);

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

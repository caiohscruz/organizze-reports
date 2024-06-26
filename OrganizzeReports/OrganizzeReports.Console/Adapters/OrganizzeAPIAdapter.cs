﻿using Microsoft.Extensions.Configuration;
using OrganizzeReports.Console.DTOs;
using OrganizzeReports.Console.Utils;
using System.Text.Json;

namespace OrganizzeReports.Console.Adapters
{
    /// <summary>
    /// Adapter class for interacting with the Organizze API.
    /// </summary>
    public class OrganizzeAPIAdapter
    {
        #region Endpoints
        private enum Endpoint
        {
            CreditCards,
            Invoices,
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
                Endpoint.Invoices => "credit_cards/{0}/invoices",
                _ => throw new ArgumentException("Unknown endpoint"),
            };

            return $"{_settings.BaseUrl}/{endpointPath}";
        }
        #endregion

        private readonly OrganizzeAPISettings _settings;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizzeAPIAdapter"/> class.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        /// <param name="email">The email of the user.</param>
        /// <param name="apiKey">The API key for accessing the Organizze API.</param>
        public OrganizzeAPIAdapter(IConfiguration configuration)
        {
            _settings = configuration.GetSection("OrganizzeApiSettings").Get<OrganizzeAPISettings>();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_settings.Email}:{_settings.ApiKey}")));
            client.DefaultRequestHeaders.Add("User-Agent", $"{_settings.UserName} ({_settings.Email})");
            _httpClient = client;
        }

        #region Requests
        /// <summary>
        /// Retrieves all categories.
        /// </summary>
        /// <returns>Returns a collection of <see cref="CategoryDTO"/>.</returns>
        public async Task<IEnumerable<CategoryDTO>> GetCategories()
        {
            return await GetEndpointData<CategoryDTO>(Endpoint.Categories);
        }

        /// <summary>
        /// Retrieves transactions based on the specified criteria.
        /// </summary>
        /// <param name="startDate">Optional. The start date of the period.</param>
        /// <param name="endDate">Optional. The end date of the period.</param>
        /// <param name="accountId">Optional. The ID of the account.</param>
        /// <returns>Returns a collection of <see cref="TransactionDTO"/> that match the specified criteria.</returns>
        /// <remarks>
        /// This method retrieves a maximum of 500 records.
        /// If dates are not specified, the method retrieves transactions from the current month.
        /// </remarks>
        public async Task<IEnumerable<TransactionDTO>> GetTransactions(DateTime? startDate = null, DateTime? endDate = null, long? accountId = null)
        {
            var queryBuilder = new QueryBuilder();
            if (startDate != null) queryBuilder.Add("start_date", startDate.Value.ToString("yyyy-MM-dd"));
            if (endDate != null) queryBuilder.Add("end_date", endDate.Value.ToString("yyyy-MM-dd"));
            if (accountId != null) queryBuilder.Add("account_id", accountId.Value.ToString());
            string queryString = queryBuilder.ToString();

            return await GetEndpointData<TransactionDTO>(Endpoint.Transactions, queryString);
        }

        /// <summary>
        /// Retrieves all accounts.
        /// </summary>
        /// <returns>Returns a collection of <see cref="AccountDTO"/>.</returns>
        public async Task<IEnumerable<AccountDTO>> GetAccounts()
        {
            return await GetEndpointData<AccountDTO>(Endpoint.Accounts);
        }

        /// <summary>
        /// Retrieves all credit cards.
        /// </summary>
        /// <returns>Returns a collection of <see cref="CreditCardDTO"/>.</returns>
        public async Task<IEnumerable<CreditCardDTO>> GetCreditCards()
        {
            return await GetEndpointData<CreditCardDTO>(Endpoint.CreditCards);
        }

        /// <summary>
        /// Retrieves all invoices of current year.
        /// </summary>
        /// <returns>Returns a collection of <see cref="InvoiceDTO"/>.</returns>
        public async Task<IEnumerable<InvoiceDTO>> GetInvoices(int id)
        {
            return await GetEndpointData<InvoiceDTO>(endpoint: Endpoint.Invoices, routeParam: id);
        }
        #endregion

        #region Utils
        private async Task<IEnumerable<T>> GetEndpointData<T>(Endpoint endpoint, string queryString = null, int? routeParam = null)
        {
            string endpointUrl = GetEndpointUrl(endpoint);

            if (!string.IsNullOrEmpty(queryString)) endpointUrl += queryString;

            if (routeParam != null) endpointUrl = string.Format(endpointUrl, routeParam);

            HttpResponseMessage response = await _httpClient.GetAsync(endpointUrl);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<T>>(json);
            }
            else
            {
                System.Console.WriteLine($"Error in request to {endpoint}: {response.StatusCode}");
                return default;
            }
        }
        #endregion
    }
}

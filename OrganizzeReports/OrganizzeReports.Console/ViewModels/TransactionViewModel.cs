using CsvHelper.Configuration.Attributes;
using System;
using System.Text.Json.Serialization;

namespace OrganizzeReports.Console.ViewModels
{
    public class TransactionViewModel
    {
        [Name("Description")] // Nome da coluna no arquivo CSV
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [Name("Date")]
        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [Name("Amount")]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [Name("TotalInstallments")]
        [JsonPropertyName("total_installments")]
        public int? TotalInstallments { get; set; }
        
        [Name("Installment")]
        [JsonPropertyName("installment")]
        public int? Installment { get; set; }

        [Name("Recurring")]
        [JsonPropertyName("recurring")]
        public bool? Recurring { get; set; }

        [Name("Account")]
        [JsonPropertyName("account")]
        public string? Account { get; set; }

        [Name("Category")]
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [Name("CreditCard")]
        [JsonPropertyName("credit_card")]
        public string? CreditCard { get; set; }
    }
}

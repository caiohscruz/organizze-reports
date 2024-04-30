using System.Text.Json.Serialization;

namespace OrganizzeReports.Console.ViewModels
{
    public class TransactionViewModel
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("total_installments")]
        public int? TotalInstallments { get; set; }
        
        [JsonPropertyName("installment")]
        public int? Installment { get; set; }

        [JsonPropertyName("recurring")]
        public bool? Recurring { get; set; }

        [JsonPropertyName("account")]
        public string? Account { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("credit_card")]
        public string? CreditCard { get; set; }
    }
}

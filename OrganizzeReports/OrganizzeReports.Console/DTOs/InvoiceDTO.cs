using System.Text.Json.Serialization;

namespace OrganizzeReports.Console.DTOs
{
    public class InvoiceDTO
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("starting_date")]
        public DateTime StartingDate { get; set; }

        [JsonPropertyName("closing_date")]
        public DateTime ClosingDate { get; set; }

        [JsonPropertyName("amount_cents")]
        public int AmountCents { get; set; }

        [JsonPropertyName("payment_amount_cents")]
        public int PaymentAmountCents { get; set; }

        [JsonPropertyName("balance_cents")]
        public int BalanceCents { get; set; }

        [JsonPropertyName("previous_balance_cents")]
        public int PreviousBalanceCents { get; set; }

        [JsonPropertyName("credit_card_id")]
        public int CreditCardId { get; set; }
    }
}




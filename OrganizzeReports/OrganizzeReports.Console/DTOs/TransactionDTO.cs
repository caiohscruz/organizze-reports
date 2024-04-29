using System.Text.Json.Serialization;

namespace OrganizzeReports.Console.DTOs
{  

    public class TransactionDTO
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("paid")]
        public bool Paid { get; set; }

        [JsonPropertyName("amount_cents")]
        public int AmountCents { get; set; }

        [JsonPropertyName("total_installments")]
        public int TotalInstallments { get; set; }

        [JsonPropertyName("installment")]
        public int Installment { get; set; }

        [JsonPropertyName("recurring")]
        public bool Recurring { get; set; }

        [JsonPropertyName("account_id")]
        public long AccountId { get; set; }

        [JsonPropertyName("category_id")]
        public long CategoryId { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("attachments_count")]
        public int AttachmentsCount { get; set; }

        [JsonPropertyName("credit_card_id")]
        public long CreditCardId { get; set; }

        [JsonPropertyName("credit_card_invoice_id")]
        public int CreditCardInvoiceId { get; set; }

        [JsonPropertyName("paid_credit_card_id")]
        public object PaidCreditCardId { get; set; }

        [JsonPropertyName("paid_credit_card_invoice_id")]
        public object PaidCreditCardInvoiceId { get; set; }

        [JsonPropertyName("oposite_transaction_id")]
        public object OpositeTransactionId { get; set; }

        [JsonPropertyName("oposite_account_id")]
        public object OpositeAccountId { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("tags")]
        public List<object> Tags { get; set; }

        [JsonPropertyName("attachments")]
        public List<object> Attachments { get; set; }

        [JsonPropertyName("recurrence_id")]
        public object RecurrenceId { get; set; }
    }

}

using System.Text.Json.Serialization;

namespace OrganizzeReports.Console.DTOs
{
    public class Account
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("institution_id")]
        public string InstitutionId { get; set; }

        [JsonPropertyName("institution_name")]
        public string InstitutionName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

}

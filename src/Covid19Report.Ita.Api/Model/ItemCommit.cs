using System.Text.Json.Serialization;

namespace Covid19Report.Ita.Api.Model
{
    public class ItemCommit
    {
        public string Id { get; set; } = default!;
        public string Sha { get; set; } = default!;
        public string? Autore { get; set; }
        public string? Data { get; set; }
        public string? Messaggio { get; set; }
        public string? Url { get; set; }

        [JsonPropertyName("partition_key")]
        public string? PartitionKey { get; set; }
    }
}

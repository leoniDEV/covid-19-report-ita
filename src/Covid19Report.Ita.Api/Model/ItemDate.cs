using System.Text.Json.Serialization;

namespace Covid19Report.Ita.Api.Model
{
    public class ItemDate
    {
        public string Id { get; set; } = default!;

        [JsonPropertyName("partition_key")]
        public string PartitionKey { get; set; } = default!;
    }
}

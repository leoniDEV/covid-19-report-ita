using System.Text.Json.Serialization;

namespace Covid19Report.Ita.Api.Model
{
    public class ItemProvincia
    {
        public string Id { get; set; } = default!;
        public string Data { get; set; } = default!;
        public string CodiceRegione { get; set; } = default!;
        public string CodiceProvincia { get; set; } = default!;
        public string? TotaleCasi { get; set; }
        public string? NoteIt { get; set; }
        public string? NoteEn { get; set; }

        [JsonPropertyName("partition_key")]
        public string PartitionKey { get; set; } = default!;
    }
}

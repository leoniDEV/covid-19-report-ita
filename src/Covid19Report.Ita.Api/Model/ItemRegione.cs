using System.Text.Json.Serialization;

namespace Covid19Report.Ita.Api.Model
{
    public class ItemRegione
    {
        public string Id { get; set; } = default!;
        public string Data { get; set; } = default!;
        public string CodiceRegione { get; set; } = default!;
        public string? RicoveratiConSintomi { get; set; }
        public string? TerapiaIntensiva { get; set; }
        public string? TotaleOspedalizzati { get; set; }
        public string? IsolamentoDomiciliare { get; set; }
        public string? TotalePositivi { get; set; }
        public string? VariazioneTotalePositivi { get; set; }
        public string? NuoviPositivi { get; set; }
        public string? DimessiGuariti { get; set; }
        public string? Deceduti { get; set; }
        public string? TotaleCasi { get; set; }
        public string? Tamponi { get; set; }
        public string? CasiTestati { get; set; }
        public string? NoteIt { get; set; }
        public string? NoteEn { get; set; }

        [JsonPropertyName("partition_key")]
        public string PartitionKey { get; set; } = default!;
    }
}

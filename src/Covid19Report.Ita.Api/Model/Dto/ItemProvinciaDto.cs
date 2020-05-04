namespace Covid19Report.Ita.Api.Model.Dto
{
    public class ItemProvinciaDto
    {
        public string Data { get; set; } = default!;
        public string Stato { get; set; } = default!;
        public string CodiceRegione { get; set; } = default!;
        public string DenominazioneRegione { get; set; } = default!;
        public string CodiceProvincia { get; set; } = default!;
        public string DenominazioneProvincia { get; set; } = default!;
        public string SiglaProvincia { get; set; } = default!;
        public string Lat { get; set; } = default!;
        public string Long { get; set; } = default!;
        public string? TotaleCasi { get; set; }
        public string? NoteIt { get; set; }
        public string? NoteEn { get; set; }
    }
}

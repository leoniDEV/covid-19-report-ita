namespace Covid19Report.Ita.Api.Model.Dto
{
    public class ItemRegioneDto
    {
        public string Data { get; set; } = default!;
        public string Stato { get; set; } = default!;
        public string CodiceRegione { get; set; } = default!;
        public string DenominazioneRegione { get; set; } = default!;
        public string Lat { get; set; } = default!;
        public string Long { get; set; } = default!;
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

        public void Deconstruct(
            out string data,
            out string stato,
            out string codiceRegione,
            out string denominazioneRegione,
            out string lat,
            out string longiture,
            out string? ricoveratiConSintomi,
            out string? terapiaIntensiva,
            out string? totaleOspedalizzati,
            out string? isolamentoDomiciliare,
            out string? totalePositivi,
            out string? variazioneTotalePositivi,
            out string? nuoviPositivi,
            out string? dimessiGuariti,
            out string? deceduti,
            out string? totaleCasi,
            out string? tamponi,
            out string? casiTestati,
            out string? noteIt,
            out string? noteEn)
        {
            data = Data;
            stato = Stato;
            codiceRegione = CodiceRegione;
            denominazioneRegione = DenominazioneRegione;
            lat = Lat;
            longiture = Long;
            ricoveratiConSintomi = RicoveratiConSintomi;
            terapiaIntensiva = TerapiaIntensiva;
            totaleOspedalizzati = TotaleOspedalizzati;
            isolamentoDomiciliare = IsolamentoDomiciliare;
            totalePositivi = totaleOspedalizzati;
            variazioneTotalePositivi = VariazioneTotalePositivi;
            nuoviPositivi = NuoviPositivi;
            dimessiGuariti = DimessiGuariti;
            deceduti = Deceduti;
            totaleCasi = TotaleCasi;
            tamponi = Tamponi;
            casiTestati = CasiTestati;
            noteIt = NoteIt;
            noteEn = NoteEn;
        }
    }
}

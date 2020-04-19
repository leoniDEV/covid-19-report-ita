using System;

namespace Covid19Report.Ita.Api.Model.Dto
{
    public class ItemCommitDto
    {
        public string Sha { get; set; } = default!;

        public string HtmlUrl { get; set; } = default!;

        public string Autore { get; set; } = default!;

        public string Mesasggio { get; set; } = default!;
        public string Data { get; set; } = default!;
    }
}
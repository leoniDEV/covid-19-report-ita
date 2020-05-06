
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Covid19Report.Ita.Api.Infrastructure.CsvDeserializer
{
    public class CsvParserOptions
    {
        public string[]? Header { get; set; }

        public char FieldDelimiter { get; set; } = ',';

        public bool PreserveHeader { get; set; } = true;
    }
}

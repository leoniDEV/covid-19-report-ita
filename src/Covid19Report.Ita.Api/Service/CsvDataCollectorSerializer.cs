using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Infrastructure;
using Covid19Report.Ita.Api.Infrastructure.CsvDeserializer;

namespace Covid19Report.Ita.Api.Service
{
    public class CsvDataCollectorSerializer : IDataCollectorSerializer
    {
        public SerializerKind SerializerKind { get => SerializerKind.CSV; }

        public CsvDataCollectorSerializer()
        {
        }

        public async Task<T> GetDataAsync<T>(string data, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            return await GetDataAsync<T>(stream, jsonSerializerOptions);
        }

        public async Task<T> GetDataAsync<T>(Stream data, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            using (data)
            {
                var header = CsvDeserializer.DeserializeAsync<T>(data);
                
            }
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> GetData1Async<T>(Stream data, CsvParserOptions? csvParserOptions = null)
        {
            using (data)
            {
                return CsvDeserializer.DeserializeAsync<T>(data, csvParserOptions);
            }
        }
    }
}

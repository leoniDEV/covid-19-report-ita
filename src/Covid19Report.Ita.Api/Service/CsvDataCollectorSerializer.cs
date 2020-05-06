using System.Collections.Generic;
using System.IO;
using System.Text;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Infrastructure;
using Covid19Report.Ita.Api.Infrastructure.CsvDeserializer;

namespace Covid19Report.Ita.Api.Service
{
    public class CsvDataCollectorSerializer : IDataCollectorSerializer
    {
        public SerializerKind SerializerKind { get => SerializerKind.CSV; }

        public IAsyncEnumerable<T> GetDataAsync<T>(string data)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            return GetDataAsync<T>(stream);
        }

        public IAsyncEnumerable<T> GetDataAsync<T>(Stream data) => CsvDeserializer.DeserializeAsync<T>(data);
    }
}

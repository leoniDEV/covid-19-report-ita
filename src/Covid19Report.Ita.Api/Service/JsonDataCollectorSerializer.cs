using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Infrastructure;

namespace Covid19Report.Ita.Api.Service
{
    public class JsonDataCollectorSerializer : IDataCollectorSerializer
    {
        public SerializerKind SerializerKind { get => SerializerKind.Json; }

        public async IAsyncEnumerable<T> GetDataAsync<T>(Stream data)
        {
            JsonSerializerOptions? options;

            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = false,
            };
            using (data)
            {
                yield return await JsonSerializer.DeserializeAsync<T>(data, options);
            }
        }

        public async IAsyncEnumerable<T> GetDataAsync<T>(string data)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            await foreach (var item in GetDataAsync<T>(stream))
            {
                yield return item;
            }
        }
    }
}

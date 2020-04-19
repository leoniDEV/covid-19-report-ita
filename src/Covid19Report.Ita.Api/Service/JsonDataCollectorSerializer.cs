using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Infrastructure;

namespace Covid19Report.Ita.Api.Service
{
    public class JsonDataCollectorSerializer : IDataCollectorSerializer
    {
        public SerializerKind SerializerKind { get => SerializerKind.Json; }

        public async Task<T> GetDateAsync<T>(Stream data, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            JsonSerializerOptions? options;

            if (!(jsonSerializerOptions is null))
            {
                options = jsonSerializerOptions;
                if (options.PropertyNamingPolicy is null)
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }
            else
            {
                options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
            }

            using (data)
            {
                return await JsonSerializer.DeserializeAsync<T>(data, options);
            }
        }

        public async Task<T> GetDateAsync<T>(string data, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            return await GetDateAsync<T>(stream, jsonSerializerOptions);
        }
    }
}

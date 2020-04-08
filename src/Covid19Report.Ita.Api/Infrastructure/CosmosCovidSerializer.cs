using System.IO;
using System.Text.Json;

using Microsoft.Azure.Cosmos;

namespace Covid19Report.Ita.Api.Infrastructure
{
    public class CosmosCovidSerializer : CosmosSerializer
    {
        private readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public override T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                return JsonSerializer.DeserializeAsync<T>(stream, options).GetAwaiter().GetResult();
            }
        }

        public override Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            JsonSerializer.SerializeAsync(stream, input, options).GetAwaiter().GetResult();
            return stream;
        }
    }
}

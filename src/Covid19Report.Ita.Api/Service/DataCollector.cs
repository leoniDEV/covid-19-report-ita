using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Abstraction.Service;
using Covid19Report.Ita.Api.Infrastructure;

namespace Covid19Report.Ita.Api.Service
{
    public class DataCollector : IDataCollector
    {
        private readonly HttpClient httpClient;
        private readonly IEnumerable<IDataCollectorSerializer> dataCollectorSerializers;
        private IDataCollectorSerializer currentCollectorSerializer = default!;

        public DataCollector(IHttpClientFactory httpClientFactory, IEnumerable<IDataCollectorSerializer> dataCollectorSerializers)
        {
            httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Covid19-ita-Report-api");
            this.dataCollectorSerializers = dataCollectorSerializers;
        }

        public async Task<T> GetDataAsync<T>(string url, SerializerKind serializerKind, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var response = await httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStreamAsync();

            currentCollectorSerializer = dataCollectorSerializers.Single(dc => dc.SerializerKind == serializerKind);

            return await currentCollectorSerializer.GetDateAsync<T>(content, jsonSerializerOptions);
        }
    }
}

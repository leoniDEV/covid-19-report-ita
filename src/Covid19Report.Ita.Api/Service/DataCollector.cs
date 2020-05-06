using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

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

        public async IAsyncEnumerable<T> GetDataAsync<T>(string url, SerializerKind serializerKind)
        {
            var response = await httpClient.GetAsync(url);
            if (response.Content is null)
            {
                yield break;
            }

            var content = await response.Content.ReadAsStreamAsync();

            currentCollectorSerializer = dataCollectorSerializers.Single(dc => dc.SerializerKind == serializerKind);

            await foreach (var item in currentCollectorSerializer.GetDataAsync<T>(content))
            {
                yield return item;
            }
        }
    }
}

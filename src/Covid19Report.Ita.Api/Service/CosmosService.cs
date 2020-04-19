using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Abstraction.Service;

using Microsoft.Azure.Cosmos;

namespace Covid19Report.Ita.Api.Service
{
    public class CosmosService : ICosmosService
    {
        private readonly Container cosmosConstainer;

        public CosmosService(Container cosmosConstainer)
        {
            this.cosmosConstainer = cosmosConstainer;
        }

        public async Task<T> GetItemAsync<T>(string id, string partitionKey)
        {
            var response = await cosmosConstainer.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
            return response.Resource;
        }

        public async Task<IEnumerable<T>> ReadDataAsync<T>()
        {
            var query = cosmosConstainer.GetItemQueryIterator<T>(new QueryDefinition("SELECT * FROM c"));
            var result = new List<T>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                result.AddRange(response.ToList());
            }

            return result;
        }

        public async Task<HttpStatusCode> CreateItemAsync<T>(T data, string partitionKey)
        {
            var response = await cosmosConstainer.CreateItemAsync(data, new PartitionKey(partitionKey));
            return response.StatusCode;
        }

        public async Task<HttpStatusCode> UpdateDataAsync<T>(T data, string partitionKey)
        {
            var response = await cosmosConstainer.UpsertItemAsync(data, new PartitionKey(partitionKey));
            return response.StatusCode;
        }
    }
}
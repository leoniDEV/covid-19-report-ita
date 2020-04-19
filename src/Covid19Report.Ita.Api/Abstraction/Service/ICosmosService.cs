using Microsoft.Azure.Cosmos;

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Covid19Report.Ita.Api.Abstraction.Service
{
    public interface ICosmosService
    {
        public Task<HttpStatusCode> CreateItemAsync<T>(T data, string partitionKey);
        public Task<IEnumerable<T>> ReadDataAsync<T>();
        public Task<T> GetItemAsync<T>(string id, string partitionKey);
        public Task<HttpStatusCode> UpdateDataAsync<T>(T data, string partitionKey);
    }
}

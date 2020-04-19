using Microsoft.Azure.Cosmos;

namespace Covid19Report.Ita.Api.Abstraction
{
    public interface ICosmosClientFactory
    {
        CosmosClient Create(string account, string masterKey, CosmosClientOptions? clientOptions = null);
        CosmosClient Create(CosmosClientOptions? clientOptions = null);
    }
}

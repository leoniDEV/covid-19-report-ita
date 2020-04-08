using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Abstraction.Service;
using Covid19Report.Ita.Api.Service;

using Microsoft.Azure.Cosmos;

namespace Covid19Report.Ita.Api.Infrastructure
{
    public class CosmosServiceFactory : ICosmosServiceFactory
    {
        public ICosmosService Create(Container container) => new CosmosService(container);
    }
}

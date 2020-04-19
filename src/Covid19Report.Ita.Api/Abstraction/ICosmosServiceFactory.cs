using Covid19Report.Ita.Api.Abstraction.Service;

using Microsoft.Azure.Cosmos;

namespace Covid19Report.Ita.Api.Abstraction
{
    public interface ICosmosServiceFactory
    {
        ICosmosService Create(Container container);
    }
}
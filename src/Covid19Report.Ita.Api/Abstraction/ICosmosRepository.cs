using Covid19Report.Ita.Api.Abstraction.Service;

using Microsoft.Azure.Cosmos;

using System.Collections.Generic;

namespace Covid19Report.Ita.Api.Abstraction
{
    public interface ICosmosRepository
    {
        IDictionary<string, KeyValuePair<Database, IDictionary<string, ICosmosService>>> CosmosServices { get; }
    }
}
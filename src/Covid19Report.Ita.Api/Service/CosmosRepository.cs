using System.Collections.Generic;

using Covid19Report.Ita.Api.Abstraction;
using Covid19Report.Ita.Api.Abstraction.Service;
using Covid19Report.Ita.Api.Infrastructure;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Covid19Report.Ita.Api.Service
{
    public class CosmosRepository : ICosmosRepository
    {
        private readonly ICosmosClientFactory cosmosClientFactory;
        private readonly ICosmosServiceFactory cosmosServiceFactory;
        private readonly IOptions<CosmosRepositoryOptions> options;
        private readonly CosmosSerializer? cosmosSerializer;

        public IDictionary<string, KeyValuePair<Database, IDictionary<string, ICosmosService>>> CosmosServices { get; }

        public CosmosRepository(ICosmosClientFactory cosmosFactory, ICosmosServiceFactory cosmosServiceFactory, IOptions<CosmosRepositoryOptions> options, CosmosSerializer cosmosSerializer)
        {
            this.cosmosClientFactory = cosmosFactory;
            this.cosmosServiceFactory = cosmosServiceFactory;
            this.options = options;

            this.cosmosSerializer = cosmosSerializer;
            CosmosServices = CreateDatabaseContainer();
        }

        private IDictionary<string, KeyValuePair<Database, IDictionary<string, ICosmosService>>> CreateDatabaseContainer()
        {
            var databaseOptions = options.Value.Databases;
            var services = new Dictionary<string, KeyValuePair<Database, IDictionary<string, ICosmosService>>>();
            var cosmosClientOptions = new CosmosClientOptions
            {
                Serializer = cosmosSerializer
            };

            var cosmosClient = cosmosClientFactory.Create(cosmosClientOptions);

            foreach (string dbName in databaseOptions.Keys)
            {
                var cosmosDB = cosmosClient.GetDatabase(dbName);

                var containerOptionsValue = databaseOptions[dbName];
                var containerDictionary = new Dictionary<string, ICosmosService>();

                foreach (string containerKind in containerOptionsValue.Keys)
                {
                    var container = cosmosDB.GetContainer(containerOptionsValue[containerKind]);

                    containerDictionary.Add(containerKind, cosmosServiceFactory.Create(container));
                }

                services.Add(dbName, new KeyValuePair<Database, IDictionary<string, ICosmosService>>(cosmosDB, containerDictionary));
            }

            return services;
        }
    }
}

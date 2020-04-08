using System;
using System.Configuration;

using Covid19Report.Ita.Api.Abstraction;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Covid19Report.Ita.Api.Infrastructure
{
    public class CosmosClientFactory : ICosmosClientFactory
    {
        private readonly IOptions<CosmosClientFactoryOptions>? options;
        public CosmosClientFactory(IOptions<CosmosClientFactoryOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.options = options;
        }

        public CosmosClientFactory()
        {
        }

        public CosmosClient Create(string account, string masterKey, CosmosClientOptions? clientOptions = null) => new CosmosClient(account, masterKey, clientOptions);
        public CosmosClient Create(CosmosClientOptions? clientOptions = null)
        {
            if (options is null)
            {
                throw new InvalidOperationException("You need to register an IOption<CosmosClientFactoryOptions> an set the approprieate values in your configuration to use this method");
            }

            if (options.Value.AccountEndpoint is null)
            {
                throw new ConfigurationErrorsException($"{nameof(options.Value.AccountEndpoint)} is not properly configuret in your configuration");
            }

            if (options.Value.MasterKey is null)
            {
                throw new ConfigurationErrorsException($"{nameof(options.Value.MasterKey)} is not properly configuret in your configuration");
            }

            return Create(options.Value.AccountEndpoint, options.Value.MasterKey, clientOptions);
        }
    }
}

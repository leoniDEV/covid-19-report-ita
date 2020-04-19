using System.Collections.Generic;

namespace Covid19Report.Ita.Api.Infrastructure
{
    public class CosmosRepositoryOptions
    {
        public Dictionary<string, Dictionary<string, string>> Databases { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }
}

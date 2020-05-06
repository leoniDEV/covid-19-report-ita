using System.Collections.Generic;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Infrastructure;

namespace Covid19Report.Ita.Api.Abstraction.Service
{
    public interface IDataCollector
    {
        IAsyncEnumerable<T> GetDataAsync<T>(string url, SerializerKind serializerKind);
    }
}
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Covid19Report.Ita.Api.Infrastructure;

namespace Covid19Report.Ita.Api.Abstraction
{
    public interface IDataCollectorSerializer
    {
        SerializerKind SerializerKind { get; }

        IAsyncEnumerable<T> GetDataAsync<T>(string data);
        IAsyncEnumerable<T> GetDataAsync<T>(Stream data);
    }
}
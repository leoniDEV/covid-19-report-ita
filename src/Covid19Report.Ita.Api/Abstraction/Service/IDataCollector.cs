using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Infrastructure;

namespace Covid19Report.Ita.Api.Abstraction.Service
{
    public interface IDataCollector
    {
        Task<T> GetDataAsync<T>(string url, SerializerKind serializerKind, JsonSerializerOptions? jsonSerializerOptions = null);
    }
}
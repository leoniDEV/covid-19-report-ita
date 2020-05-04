using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Covid19Report.Ita.Api.Infrastructure;

namespace Covid19Report.Ita.Api.Abstraction
{
    public interface IDataCollectorSerializer
    {
        SerializerKind SerializerKind { get; }
        Task<T> GetDataAsync<T>(string data, JsonSerializerOptions? jsonSerializerOptions = null);
        Task<T> GetDataAsync<T>(Stream data, JsonSerializerOptions? jsonSerializerOptions = null);
    }
}
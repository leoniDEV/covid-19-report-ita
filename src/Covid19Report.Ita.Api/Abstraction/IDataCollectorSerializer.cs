using Covid19Report.Ita.Api.Infrastructure;

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Covid19Report.Ita.Api.Abstraction
{
    public interface IDataCollectorSerializer
    {
        SerializerKind SerializerKind { get; }
        Task<T> GetDateAsync<T>(string data, JsonSerializerOptions? jsonSerializerOptions = null);
        Task<T> GetDateAsync<T>(Stream data, JsonSerializerOptions? jsonSerializerOptions = null);
    }
}
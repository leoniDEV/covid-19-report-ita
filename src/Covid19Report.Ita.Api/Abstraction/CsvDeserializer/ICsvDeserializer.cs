using System.Collections.Generic;
using System.IO;

namespace Covid19Report.Ita.Api.Abstraction.Deserializer
{
    public interface ICsvDeserializer<out T>
    {
        IAsyncEnumerable<T> DeserializeAsync(Stream data);
    }
}
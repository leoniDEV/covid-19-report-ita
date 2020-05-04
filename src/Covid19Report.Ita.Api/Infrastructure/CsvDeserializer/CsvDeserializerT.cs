using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Threading.Tasks;

using Covid19Report.Ita.Api.Abstraction.Deserializer;

namespace Covid19Report.Ita.Api.Infrastructure.CsvDeserializer
{
    public class CsvDeserializer<T> : ICsvDeserializer<T>
    {
        private readonly Type destinationType;
        private readonly ReadOnlyMemory<PropertyInfo> properties;
        private readonly CsvParserOptions options;
        private readonly byte fieldDelimiter;
#pragma warning disable IDE0032 // Use auto property
        private readonly int fieldCount;
#pragma warning restore IDE0032 // Use auto property
        private string?[]? headerCache;

        public int FieldCount { get => fieldCount; }

        public CsvDeserializer(CsvParserOptions? options = null)
        {
            this.options = options ?? new CsvParserOptions();
            destinationType = typeof(T);
            fieldDelimiter = (byte)this.options.FieldDelimiter;
            properties = destinationType.GetProperties().AsMemory();
            fieldCount = properties.Length;
        }

        public async IAsyncEnumerable<T> DeserializeAsync(Stream data)
        {
            var pipeReader = PipeReader.Create(data);
            Memory<string?>? headerMemory = headerCache?.AsMemory().Slice(0, fieldCount);
            if (headerCache is null)
            {
                headerCache = ArrayPool<string?>.Shared.Rent(fieldCount);
                Array.Clear(headerCache, 0, fieldCount);
                headerMemory = headerCache.AsMemory().Slice(0, fieldCount);
                var result = await CsvDeserializer.ParseLineAsync(pipeReader, headerMemory.Value, true, fieldDelimiter);

                if (result.isCancelled)
                {
                    await pipeReader.CompleteAsync();
                    ArrayPool<string?>.Shared.Return(headerCache);
                    yield break;
                }
            }

            string?[] record = ArrayPool<string?>.Shared.Rent(fieldCount);
            Array.Clear(record, 0, record.Length);
            var recordMemory = record.AsMemory().Slice(0, fieldCount);
            while (true)
            {
                (bool isCancelled, bool isCompleted) result;
                try
                {
                    result = await CsvDeserializer.ParseLineAsync(pipeReader, recordMemory, false, fieldDelimiter);
                    if (result.isCancelled)
                    {
                        break;
                    }
                }
                catch (ArgumentNullException)
                {
                    await pipeReader.CompleteAsync();
                    ArrayPool<string?>.Shared.Return(headerCache);
                    ArrayPool<string?>.Shared.Return(record);
                    throw;
                }

                object? istance = Activator.CreateInstance(typeof(T));
                for (int i = 0; i < fieldCount; i++)
                {
                    var currentProperty = properties.Span[i];
                    int fieldIndex = headerMemory!.Value.Span!.IndexOf<string>(currentProperty.Name);
                    currentProperty.SetValue(istance, recordMemory.Span[fieldIndex]);
                }

                if (istance != null)
                {
                    recordMemory.Span.Clear();
                    yield return (T)istance;
                }

                if (result.isCompleted)
                {
                    break;
                }
            }

            await pipeReader.CompleteAsync();
            ArrayPool<string?>.Shared.Return(headerCache);
            ArrayPool<string?>.Shared.Return(record);
        }
    }
}
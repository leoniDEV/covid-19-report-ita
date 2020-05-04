using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Covid19Report.Ita.Api.Infrastructure.CsvDeserializer
{
    public static class CsvDeserializer
    {
        public static IAsyncEnumerable<T> DeserializeAsync<T>(Stream data, CsvParserOptions? parsserOptions = null)
        {
            var deserializer = Create<T>(parsserOptions);
            return deserializer.DeserializeAsync(data);
        }

        internal static async ValueTask<(bool isCancelled, bool isCompleted)> ParseLineAsync(PipeReader reader, Memory<string?> lineBuffer, bool isHeader = false, byte fieldDelimiter = 44)
        {
            int index = 0;
            var readResult = await reader.ReadAsync();
            while (true)
            {
                if (readResult.IsCanceled)
                {
                    break;
                }

                var buffer = readResult.Buffer;
                var (isEol, eolPosition) = CsvParser.FindEol(buffer);

                if (readResult.IsCompleted && !isEol)
                {
                    isEol = true;
                }

                var recordBuffer = buffer.Slice(buffer.Start, eolPosition ?? buffer.End);

                ParseLine(ref recordBuffer, lineBuffer.Span, ref index, isEol, isHeader, fieldDelimiter);

                if (isEol && recordBuffer.IsEmpty)
                {
                    var advance = eolPosition is null ? buffer.End : buffer.GetPosition(Environment.NewLine.Length, eolPosition.Value);

                    reader.AdvanceTo(advance);
                    break;
                }

                reader.AdvanceTo(recordBuffer.Start, recordBuffer.End);
                readResult = await reader.ReadAsync();
            }

            return (readResult.IsCanceled, readResult.IsCompleted);
        }

        private static void ParseLine(ref ReadOnlySequence<byte> lineBuffer, Span<string?> fieldResult, ref int index, bool isEol, bool isHeader = false, byte fieldDelimiter = 44)
        {
            var sequenceReader = new SequenceReader<byte>(lineBuffer);
            char[] fieldBuffer = ArrayPool<char>.Shared.Rent(64);
            do
            {
                var fieldSpan = CsvParser.ParseField(ref sequenceReader, fieldDelimiter, isEol, fieldBuffer.AsSpan());
                if (fieldSpan.Length == 1 && fieldSpan[0] == Char.MinValue)
                {
                    if (!isHeader && sequenceReader.End)
                    {
                        index++;
                    }

                    break;
                }

                if (isHeader)
                {
                    Span<char> charsResult = stackalloc char[fieldSpan.Length];
                    try
                    {
                        fieldResult[index++] = fieldSpan == String.Empty ? null : new string(FormatField(fieldSpan, charsResult));
                    }
                    catch (ArgumentNullException)
                    {
                        ArrayPool<char>.Shared.Return(fieldBuffer);
                        throw;
                    }
                }
                else
                {
                    fieldResult[index++] = fieldSpan.IsEmpty ? null : new string(fieldSpan);
                }
            } while (!sequenceReader.End);

            ArrayPool<char>.Shared.Return(fieldBuffer);
            lineBuffer = lineBuffer.Slice(sequenceReader.Position);
        }

        private static ReadOnlySpan<char> FormatField(ReadOnlySpan<char> fieldSpan, Span<char> charsResult)
        {
            if (fieldSpan.IsEmpty)
            {
                throw new ArgumentNullException($"The header cannot have null values {nameof(fieldSpan)}");
            }

            fieldSpan.CopyTo(charsResult);
            int iterations = 0;
            while (true)
            {
                int underscoreIndex = charsResult.IndexOf('_');
                if (underscoreIndex == -1)
                {
                    break;
                }

                int next = underscoreIndex + 1;
                fieldSpan.Slice(next + iterations++, 1).ToUpperInvariant(charsResult.Slice(next, 1));
                charsResult.Slice(next).CopyTo(charsResult.Slice(underscoreIndex));
            }

            fieldSpan.Slice(0, 1).ToUpperInvariant(charsResult.Slice(0, 1));
            return charsResult.Slice(0, charsResult.Length - iterations);
        }

        private static CsvDeserializer<T> Create<T>(CsvParserOptions? parserOptions = null)
            => new CsvDeserializer<T>(parserOptions);
    }
}

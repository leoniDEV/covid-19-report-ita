using System;
using System.Buffers;
using System.Text;

namespace Covid19Report.Ita.Api.Infrastructure.CsvDeserializer
{
    internal static class CsvParser
    {
        public static ReadOnlySpan<char> ParseField(ref SequenceReader<byte> sequenceReader, byte delimiter, bool isEol, Span<char> readResult)
        {
            if (!sequenceReader.TryReadTo(out ReadOnlySpan<byte> readByte, delimiter))
            {
                if (!isEol)
                {
                    readResult[0] = Char.MinValue;
                    return readResult.Slice(0, 1);
                }

                int lenght = Encoding.UTF8.GetChars(sequenceReader.UnreadSequence, readResult);
                sequenceReader.AdvanceToEnd();
                return readResult.Slice(0, lenght).TrimEnd('\r');
            }

            return readResult.Slice(0, Encoding.UTF8.GetChars(readByte.TrimEnd((byte)13), readResult));
        }

        public static (bool isFound, SequencePosition? position) FindEol(in ReadOnlySequence<byte> buffer)
        {
            var eolPosition = buffer.PositionOf((byte)10);
            return eolPosition is SequencePosition ? (true, eolPosition) : (false, eolPosition);
        }
    }
}
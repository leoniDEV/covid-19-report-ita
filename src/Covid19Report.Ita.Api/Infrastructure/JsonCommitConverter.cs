using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

using Covid19Report.Ita.Api.Model.Dto;

namespace Covid19Report.Ita.Api.Infrastructure
{
    public class JsonCommitConverter : JsonConverter<IEnumerable<ItemCommitDto>>
    {
        public override IEnumerable<ItemCommitDto> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var jsonRoot = jsonDoc.RootElement;

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new[] { Convert(jsonRoot) };
            }

            var jsonList = jsonRoot.EnumerateArray();
            var result = new List<ItemCommitDto>();

            foreach (var item in jsonList)
            {
                result.Add(Convert(item));
            }

            return result;
        }

        private ItemCommitDto Convert(JsonElement jsonElement)
        {
            var commitDoc = jsonElement.GetProperty("commit");
            var authorDoc = commitDoc.GetProperty("author");

            return new ItemCommitDto
            {
                Sha = jsonElement.GetProperty("sha").GetString(),
                HtmlUrl = jsonElement.GetProperty("html_url").GetString(),
                Autore = authorDoc.GetProperty("name").GetString(),
                Data = authorDoc.GetProperty("date").GetDateTime(),
                Mesasggio = commitDoc.GetProperty("message").GetString()
            };
        }

        public override void Write(Utf8JsonWriter writer, [DisallowNull] IEnumerable<ItemCommitDto> value, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}

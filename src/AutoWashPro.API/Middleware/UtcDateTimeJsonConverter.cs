using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoWashPro.API.Middleware
{
    public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String &&
                DateTime.TryParse(reader.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date))
            {
                return date;
            }
            return reader.GetDateTime().ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // EF Core + legacy Npgsql behavior reads timestamps back with Kind = Unspecified,
            // so we force it to UTC before serializing to avoid the FE misinterpreting it as local time.
            var utcValue = value.Kind switch
            {
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => value
            };

            writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using DatingClient.Models;

namespace DatingClient.Converters;

public class GenderJsonConverter : JsonConverter<Gender>
{
    public override Gender Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return Gender.Unknown;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()?.Trim().ToLowerInvariant();
            return str switch
            {
                "male" or "Male" or "m" or "1" => Gender.Male,
                "female" or "Female" or "f" or "2" => Gender.Female,
                _ => Gender.Unknown
            };
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var num = reader.GetInt32();
            return num switch
            {
                1 => Gender.Male,
                2 => Gender.Female,
                _ => Gender.Unknown
            };
        }

        return Gender.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, Gender value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}

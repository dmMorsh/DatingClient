using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DatingClient.Helpers;
public static class QueryHelper
{
    public static Dictionary<string, string?> ToQueryDictionary<T>(T obj)
    {
        var dict = new Dictionary<string, string?>();
        if (obj is null)
            return dict;

        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj);
            if (value is null)
                continue;

            // search for backing field to get attributes
            var field = typeof(T).GetField($"_{char.ToLowerInvariant(prop.Name[0])}{prop.Name.Substring(1)}",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var jsonAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>()
                           ?? field?.GetCustomAttribute<JsonPropertyNameAttribute>();

            if (jsonAttr is null)
            {
                if (field?.GetCustomAttribute<JsonIgnoreAttribute>() is not null) continue;
            };
            var key = jsonAttr?.Name ?? prop.Name;
            
            // get converter if any
            var converterAttr = prop.GetCustomAttribute<JsonConverterAttribute>()
                                ?? field?.GetCustomAttribute<JsonConverterAttribute>();
            
            if (converterAttr != null)
            {
                var converter = (JsonConverter)Activator.CreateInstance(converterAttr.ConverterType)!;

                var json = JsonSerializer.Serialize(value, value.GetType(), new JsonSerializerOptions
                {
                    Converters = { converter }
                });

                dict[key] = json.Trim('"');
            }
            else
            {
                dict[key] = value.ToString();
            }
            
        }

        return dict;
    }
    
    public static async Task<string> BuildQueryStringAsync(Dictionary<string, string?> query)
    {
        var content = new FormUrlEncodedContent(query);
        var qs = await content.ReadAsStringAsync(); // returns "a=1&b=2"
        return qs;
    }
}

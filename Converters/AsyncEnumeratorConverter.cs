using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ResumableFunctions.Converters;

internal class AsyncEnumeratorConverter : JsonConverter
{
    public override bool CanRead => true;
    public override bool CanWrite => true;

    public override bool CanConvert(Type objectType)
    {
        return objectType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)) != null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        List<string> privateNames = new();
        writer.WriteStartObject();
        var fields = value?.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public) ?? [];
        foreach (var field in fields)
        {
            // write public fields
            writer.WritePropertyName(field.Name);
            serializer.Serialize(writer, field.GetValue(value));
            if (Regex.Match(field.Name, @"^<>\d+__(.+)$") is { Success: true } match)
            {
                privateNames.Add(match.Groups[1].Value);
            }
        }
        fields = value?.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic) ?? [];
        foreach (var field in fields.Where(f => privateNames.Contains(f.Name, StringComparer.InvariantCulture)))
        {
            // write filtered private fields
            writer.WritePropertyName(field.Name);
            serializer.Serialize(writer, field.GetValue(value));
        }
        writer.WriteEndObject();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize(reader, objectType); // tohle neubde fungovat pro privátní fieldy
    }
}

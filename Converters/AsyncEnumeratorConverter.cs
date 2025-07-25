﻿using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ResumableFunctions.Converters;

internal class AsyncEnumeratorConverter : JsonConverter
{
    private readonly JsonSerializer nonPublicSerializer;

    public override bool CanRead => true;
    public override bool CanWrite => true;

    public AsyncEnumeratorConverter()
    {
        nonPublicSerializer = JsonSerializer.Create(
            new()
            {
                ContractResolver = new DefaultContractResolver()
                {
                    // TODO: custom contract resolver
                    DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                }
            }
        );
    }

    public AsyncEnumeratorConverter(JsonSerializerSettings innerSerializerSettings) : this(JsonSerializer.Create(innerSerializerSettings)) { }

    public AsyncEnumeratorConverter(JsonSerializer innerSerializer)
    {
        nonPublicSerializer = innerSerializer;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)) != null;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }
        Type valueType = value.GetType();
        List<string> privateNames = new();
        writer.WriteStartObject();
        var fields = valueType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        FieldInfo? thisField = fields.SingleOrDefault(f => f.FieldType == valueType.DeclaringType);
        foreach (var field in fields)
        {
            // write public fields
            writer.WritePropertyName(field.Name);
            if (field == thisField)
            {
                nonPublicSerializer.Serialize(writer, field.GetValue(value));
            }
            else
            {
                serializer.Serialize(writer, field.GetValue(value));
            }
            if (Regex.Match(field.Name, @"^<>\d+__(.+)$", RegexOptions.Compiled) is { Success: true } match)
            {
                privateNames.Add(match.Groups[1].Value);
            }
        }
        fields = valueType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
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

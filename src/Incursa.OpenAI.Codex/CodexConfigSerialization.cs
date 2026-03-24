using System.Text.Json.Nodes;
using System.Text.Json;

namespace Incursa.OpenAI.Codex;

internal static class CodexConfigSerialization
{
    public static IReadOnlyList<string> FlattenConfigOverrides(CodexConfigObject? config)
    {
        if (config is null)
        {
            return [];
        }

        List<string> overrides = new();
        FlattenConfigOverrides(config, prefix: "", overrides);
        return overrides;
    }

    public static string ToTomlLiteral(JsonNode? value, string path)
    {
        return value switch
        {
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out string? stringValue) => ToTomlLiteral(stringValue, path),
            JsonValue jsonValue when jsonValue.TryGetValue<bool>(out bool boolValue) => boolValue ? "true" : "false",
            JsonValue jsonValue when jsonValue.TryGetValue<double>(out double numberValue) => ToTomlLiteral(numberValue, path),
            JsonObject jsonObject => ToTomlLiteral(jsonObject, path),
            JsonArray jsonArray => $"[{string.Join(", ", jsonArray.Select((item, index) => ToTomlLiteral(item, $"{path}[{index}]")))}]",
            null => throw new InvalidOperationException($"Codex config override at {path} cannot be null"),
            _ => throw new InvalidOperationException($"Unsupported Codex config override value at {path}"),
        };
    }

    public static string ToTomlLiteral(CodexConfigValue value, string path)
    {
        return value switch
        {
            CodexConfigStringValue stringValue => ToTomlLiteral(stringValue.Value, path),
            CodexConfigNumberValue numberValue => ToTomlLiteral(numberValue.Value, path),
            CodexConfigBooleanValue booleanValue => booleanValue.Value ? "true" : "false",
            CodexConfigArrayValue arrayValue => $"[{string.Join(", ", arrayValue.Items.Select((item, index) => ToTomlLiteral(item, $"{path}[{index}]")))}]",
            CodexConfigObject objectValue => ToTomlLiteral(objectValue.Values, path),
            _ => throw new InvalidOperationException($"Unsupported Codex config override value at {path}"),
        };
    }

    private static string ToTomlLiteral(string value, string path)
        => JsonSerializer.Serialize(value);

    private static string ToTomlLiteral(double value, string path)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException($"Codex config override at {path} must be a finite number");
        }

        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ToTomlLiteral(IReadOnlyDictionary<string, CodexConfigValue> value, string path)
    {
        if (value.Count == 0)
        {
            return "{}";
        }

        List<string> parts = new(value.Count);
        foreach (KeyValuePair<string, CodexConfigValue> pair in value)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new InvalidOperationException("Codex config override keys must be non-empty strings.");
            }

            parts.Add($"{FormatTomlKey(pair.Key)} = {ToTomlLiteral(pair.Value, $"{path}.{pair.Key}")}");
        }

        return $"{{{string.Join(", ", parts)}}}";
    }

    private static void FlattenConfigOverrides(CodexConfigObject config, string prefix, List<string> overrides)
    {
        foreach (KeyValuePair<string, CodexConfigValue> pair in config.Values)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new InvalidOperationException("Codex config override keys must be non-empty strings.");
            }

            string nextPath = string.IsNullOrWhiteSpace(prefix)
                ? pair.Key
                : $"{prefix}.{pair.Key}";

            switch (pair.Value)
            {
                case CodexConfigObject objectValue when objectValue.Values.Count == 0:
                    overrides.Add($"{nextPath}={{}}");
                    break;
                case CodexConfigObject objectValue:
                    FlattenConfigOverrides(objectValue, nextPath, overrides);
                    break;
                case null:
                    throw new InvalidOperationException($"Codex config override at {nextPath} cannot be null");
                default:
                    overrides.Add($"{nextPath}={ToTomlLiteral(pair.Value, nextPath)}");
                    break;
            }
        }
    }

    private static string FormatTomlKey(string key)
    {
        return key.All(static c => char.IsLetterOrDigit(c) || c is '_' or '-')
            ? key
            : JsonSerializer.Serialize(key);
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;

namespace JSON2CSV.Client.Models;

public class JsonInputModel
{
    public string JsonText { get; set; } = string.Empty;

    public bool IsValidFormat { get; set; }

    public object? ParsedObject { get; set; }

    public int SizeInBytes => string.IsNullOrEmpty(JsonText) ? 0 : System.Text.Encoding.UTF8.GetByteCount(JsonText);

    public bool IsEmpty => string.IsNullOrWhiteSpace(JsonText);

    public bool ContainsSensitiveData()
    {
        var sensitivePatterns = new[]
        {
            @"[""']?senha[""']?",
            @"[""']?password[""']?",
            @"[""']?token[""']?",
            @"[""']?api[_-]?key[""']?",
            @"[""']?chave[""']?",
            @"[""']?api[_-]?token[""']?",
            @"[""']?secret[""']?",
            @"[""']?auth[""']?",
            @"[""']?credential[""']?",
            @"[""']?senha[""']?",
            @"[""']?cpf[""']?",
            @"[""']?cnpj[""']?",
            @"[""']?cartao[""']?",
            @"[""']?credit[_-]?card[""']?",
            @"[""']?cvv[""']?",
            @"[""']?pin[""']?"
        };

        foreach (var pattern in sensitivePatterns)
        {
            if (Regex.IsMatch(JsonText, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public string MaskSensitiveData(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText))
            return jsonText;

        try
        {
            using var document = JsonDocument.Parse(jsonText);
            var maskedJson = MaskElement(document.RootElement);
            return maskedJson;
        }
        catch
        {
            return jsonText;
        }
    }

    private string MaskElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, string>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name.ToLower();
                    var value = property.Value.ValueKind == JsonValueKind.String
                        ? MaskValue(property.Name, property.Value.GetString() ?? string.Empty)
                        : MaskElement(property.Value);

                    obj[property.Name] = value;
                }
                return JsonSerializer.Serialize(obj);

            case JsonValueKind.Array:
                var array = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(MaskElement(item));
                }
                return JsonSerializer.Serialize(array);

            case JsonValueKind.String:
                return $"\"{MaskValue("", element.GetString() ?? string.Empty)}\"";

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return element.GetRawText() ?? "";

            default:
                return element.ToString() ?? "";
        }
    }

    private string MaskValue(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var keyLower = key.ToLower();

        var sensitiveKeys = new[]
        {
            "senha", "password", "token", "api_key", "chave", "api_token",
            "secret", "auth", "credential", "cpf", "cnpj", "cartao",
            "credit_card", "cvv", "pin", "senha_confirmacao", "password_confirmation"
        };

        foreach (var sensitive in sensitiveKeys)
        {
            if (keyLower.Contains(sensitive))
            {
                if (value.Length > 4)
                {
                    return new string('*', value.Length - 4) + value.Substring(value.Length - 4);
                }
                return new string('*', value.Length);
            }
        }

        return value;
    }
}

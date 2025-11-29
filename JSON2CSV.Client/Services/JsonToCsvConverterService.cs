using System.Text;
using System.Text.Json;
using JSON2CSV.Client.Models;

namespace JSON2CSV.Client.Services;

public class JsonToCsvConverterService : IJsonToCsvConverterService
{
    private const char CsvSeparator = ',';
    private const string Quote = "\"";
    private const string DoubleQuote = "\"\"";
    private const string NewLine = "\r\n";
    private const string NestedSeparator = ".";

    private const int MaxArrayElements = 100;
    private const int MaxOutputRows = 10000;
    private const int MaxColumns = 500;

    public ConversionResult Convert(string jsonText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return ConversionResult.Failure(
                    "O JSON está vazio.",
                    ConversionErrorType.EmptyJson
                );
            }

            var inputModel = new JsonInputModel { JsonText = jsonText };
            string maskedJson = jsonText;

            if (inputModel.ContainsSensitiveData())
            {
                maskedJson = inputModel.MaskSensitiveData(jsonText);
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(maskedJson);
            }
            catch (JsonException)
            {
                return ConversionResult.Failure(
                    "JSON inválido.",
                    ConversionErrorType.InvalidJson
                );
            }

            using (document)
            {
                return Convert(document.RootElement);
            }
        }
        catch (Exception ex)
        {
            return ConversionResult.Failure(
                $"Erro na conversão: {ex.Message}",
                ConversionErrorType.ConversionError
            );
        }
    }

    public ConversionResult Convert(object? jsonObject)
    {
        if (jsonObject is JsonElement element)
        {
            return Convert(element);
        }

        return ConversionResult.Failure(
            "Objeto JSON inválido.",
            ConversionErrorType.InvalidJson
        );
    }

    private ConversionResult Convert(JsonElement element)
    {
        var csv = new StringBuilder();

        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                return ConvertArray(element, csv);

            case JsonValueKind.Object:
                return ConvertObject(element, csv);

            default:
                return ConversionResult.Failure(
                    "JSON deve ser um array de objetos ou um objeto simples.",
                    ConversionErrorType.InvalidJson
                );
        }
    }

    private ConversionResult ConvertArray(JsonElement array, StringBuilder csv)
    {
        if (array.GetArrayLength() == 0)
        {
            return ConversionResult.Failure(
                "Array JSON vazio.",
                ConversionErrorType.InvalidJson
            );
        }

        var items = array.EnumerateArray().ToList();

        if (items.Any(item => item.ValueKind != JsonValueKind.Object))
        {
            return ConversionResult.Failure(
                "Apenas arrays de objetos são suportados.",
                ConversionErrorType.InvalidJson
            );
        }

        var allRows = new List<Dictionary<string, string>>();
        var allKeys = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            var expandedRows = ExpandObjectToRows(item);

            foreach (var row in expandedRows)
            {
                allRows.Add(row);
                foreach (var key in row.Keys)
                {
                    allKeys.Add(key);

                    if (allKeys.Count > MaxColumns)
                    {
                        return ConversionResult.Failure(
                            $"CSV teria {allKeys.Count} colunas. Máximo permitido: {MaxColumns}",
                            ConversionErrorType.ConversionError
                        );
                    }
                }
            }
        }

        var headers = allKeys.ToList();

        csv.AppendLine(string.Join(CsvSeparator.ToString(), headers.Select(EscapeCsvField)));

        foreach (var row in allRows)
        {
            var values = new List<string>();
            foreach (var header in headers)
            {
                if (row.TryGetValue(header, out var value))
                {
                    values.Add(EscapeCsvField(value));
                }
                else
                {
                    values.Add(string.Empty);
                }
            }
            csv.AppendLine(string.Join(CsvSeparator.ToString(), values));
        }

        var outputLines = csv.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (outputLines.Length > MaxOutputRows + 1)
        {
            return ConversionResult.Failure(
                $"CSV teria {outputLines.Length - 1} linhas de dados. Máximo permitido: {MaxOutputRows}",
                ConversionErrorType.ConversionError
            );
        }

        return ConversionResult.Success(csv.ToString());
    }

    private ConversionResult ConvertObject(JsonElement obj, StringBuilder csv)
    {
        var expandedRows = ExpandObjectToRows(obj);
        var allKeys = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var row in expandedRows)
        {
            foreach (var key in row.Keys)
            {
                allKeys.Add(key);
            }
        }

        var headers = allKeys.ToList();

        csv.AppendLine(string.Join(CsvSeparator.ToString(), headers.Select(EscapeCsvField)));

        foreach (var row in expandedRows)
        {
            var values = new List<string>();
            foreach (var header in headers)
            {
                if (row.TryGetValue(header, out var value))
                {
                    values.Add(EscapeCsvField(value));
                }
                else
                {
                    values.Add(string.Empty);
                }
            }
            csv.AppendLine(string.Join(CsvSeparator.ToString(), values));
        }

        return ConversionResult.Success(csv.ToString());
    }

    private List<Dictionary<string, string>> ExpandObjectToRows(JsonElement element, string prefix = "", int depth = 0)
    {
        if (depth > 50)
        {
            return new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { prefix, "[Objeto muito profundo]" } }
            };
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { prefix, FormatJsonElement(element) } }
            };
        }

        var rows = new List<Dictionary<string, string>> { new Dictionary<string, string>() };

        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix)
                ? property.Name
                : $"{prefix}{NestedSeparator}{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var nestedRows = ExpandObjectToRows(property.Value, key, depth + 1);
                rows = CartesianProduct(rows, nestedRows);
            }
            else if (property.Value.ValueKind == JsonValueKind.Array)
            {
                var arrayRows = ExpandArray(property.Value, key, depth);
                rows = CartesianProduct(rows, arrayRows);
            }
            else
            {
                var value = FormatJsonElement(property.Value);
                foreach (var row in rows)
                {
                    row[key] = value;
                }
            }
        }

        return rows;
    }

    private List<Dictionary<string, string>> ExpandArray(JsonElement arrayElement, string key, int depth)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array)
        {
            return new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { key, "" } }
            };
        }

        var items = arrayElement.EnumerateArray().ToList();

        if (items.Count == 0)
        {
            return new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { key, "" } }
            };
        }

        if (items.Count > MaxArrayElements)
        {
            return new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { key, $"[Array com {items.Count} elementos - limite: {MaxArrayElements}]" } }
            };
        }

        bool allPrimitives = items.All(item =>
            item.ValueKind != JsonValueKind.Object &&
            item.ValueKind != JsonValueKind.Array);

        if (allPrimitives)
        {
            var values = items.Select(FormatJsonElement);
            var joined = string.Join("; ", values);
            return new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { key, joined } }
            };
        }
        else
        {
            var allRows = new List<Dictionary<string, string>>();

            foreach (var item in items)
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var expandedRows = ExpandObjectToRows(item, key, depth + 1);
                    allRows.AddRange(expandedRows);

                    if (allRows.Count > MaxOutputRows)
                    {
                        return new List<Dictionary<string, string>>
                        {
                            new Dictionary<string, string> { { key, $"[Resultado truncado - limite: {MaxOutputRows} linhas]" } }
                        };
                    }
                }
                else if (item.ValueKind == JsonValueKind.Array)
                {
                    var nestedRows = ExpandArray(item, key, depth + 1);
                    allRows.AddRange(nestedRows);

                    if (allRows.Count > MaxOutputRows)
                    {
                        return new List<Dictionary<string, string>>
                        {
                            new Dictionary<string, string> { { key, $"[Resultado truncado - limite: {MaxOutputRows} linhas]" } }
                        };
                    }
                }
                else
                {
                    allRows.Add(new Dictionary<string, string> { { key, FormatJsonElement(item) } });
                }
            }

            return allRows.Count > 0 ? allRows : new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { key, "" } }
            };
        }
    }

    private List<Dictionary<string, string>> CartesianProduct(
        List<Dictionary<string, string>> rowsA,
        List<Dictionary<string, string>> rowsB)
    {
        if (rowsB.Count == 0)
        {
            return rowsA;
        }

        if (rowsA.Count == 0)
        {
            return rowsB;
        }

        var result = new List<Dictionary<string, string>>();

        foreach (var rowA in rowsA)
        {
            foreach (var rowB in rowsB)
            {
                var combined = new Dictionary<string, string>(rowA);
                foreach (var kvp in rowB)
                {
                    combined[kvp.Key] = kvp.Value;
                }
                result.Add(combined);
            }
        }

        return result;
    }

    public string FormatValue(object? value)
    {
        switch (value)
        {
            case JsonElement element:
                return FormatJsonElement(element);
            case null:
                return string.Empty;
            case string str:
                return str;
            case bool b:
                return b.ToString().ToLower();
            case int i:
                return i.ToString();
            case long l:
                return l.ToString();
            case double d:
                return d.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            case float f:
                return f.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            case decimal dc:
                return dc.ToString(System.Globalization.CultureInfo.InvariantCulture);
            default:
                return value?.ToString() ?? string.Empty;
        }
    }

    private string FormatJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;

            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                    return intValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (element.TryGetInt64(out long longValue))
                    return longValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (element.TryGetDouble(out double doubleValue))
                    return doubleValue.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
                if (element.TryGetDecimal(out decimal decimalValue))
                    return decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return element.GetRawText() ?? string.Empty;

            case JsonValueKind.True:
                return "true";

            case JsonValueKind.False:
                return "false";

            case JsonValueKind.Null:
                return string.Empty;

            default:
                return string.Empty;
        }
    }

    public string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string sanitizedValue = SanitizeCsvFormula(value);

        bool needsQuotes = sanitizedValue.Contains(CsvSeparator) ||
                         sanitizedValue.Contains(Quote) ||
                         sanitizedValue.Contains('\n') ||
                         sanitizedValue.Contains('\r') ||
                         sanitizedValue.StartsWith("=") ||
                         sanitizedValue.StartsWith("+") ||
                         sanitizedValue.StartsWith("-") ||
                         sanitizedValue.StartsWith("@");

        if (needsQuotes)
        {
            string escapedValue = sanitizedValue.Replace(Quote, DoubleQuote);
            return Quote + escapedValue + Quote;
        }

        return sanitizedValue;
    }

    private string SanitizeCsvFormula(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var dangerousPrefixes = new[]
        {
            "=", "+", "-", "@"
        };

        foreach (var prefix in dangerousPrefixes)
        {
            if (value.StartsWith(prefix))
            {
                return "'" + value;
            }
        }

        if (value.Contains("DDE") || value.Contains("CMD"))
        {
            return "'" + value;
        }

        return value;
    }
}

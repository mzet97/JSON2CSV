using JSON2CSV.Client.Models;
using System.Text.Json;

namespace JSON2CSV.Client.Services;

public class JsonValidatorService : IJsonValidatorService
{
    private const int MaxJsonSizeInBytes = 10 * 1024 * 1024;
    private const int MaxDepth = 10;
    private const int MaxTokenCount = 100000;

    public ConversionResult Validate(string jsonText)
    {
        if (IsEmpty(jsonText))
        {
            return ConversionResult.Failure(
                "O campo JSON está vazio. Por favor, insira um JSON válido.",
                ConversionErrorType.EmptyJson
            );
        }

        if (IsTooLarge(jsonText))
        {
            return ConversionResult.Failure(
                $"O JSON fornecido é muito grande (máximo: {MaxJsonSizeInBytes / (1024 * 1024)}MB).",
                ConversionErrorType.TooLarge
            );
        }

        if (!IsValidJson(jsonText, out string? errorDetail))
        {
            return ConversionResult.Failure(
                $"JSON inválido: {errorDetail}",
                ConversionErrorType.InvalidJson
            );
        }

        return ConversionResult.Success(string.Empty);
    }

    public bool IsEmpty(string jsonText)
    {
        return string.IsNullOrWhiteSpace(jsonText);
    }

    public bool IsValidJson(string jsonText, out string? errorDetail)
    {
        errorDetail = null;
        try
        {
            var options = new JsonDocumentOptions
            {
                MaxDepth = MaxDepth,
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false
            };

            using var document = JsonDocument.Parse(jsonText, options);

            int tokenCount = 0;
            CountTokens(document.RootElement, ref tokenCount);

            if (tokenCount > MaxTokenCount)
            {
                errorDetail = $"JSON contém muitos elementos ({tokenCount}). Máximo permitido: {MaxTokenCount}";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            errorDetail = $"Erro de sintaxe na linha {ex.LineNumber}, posição {ex.BytePositionInLine}";
            return false;
        }
        catch (ArgumentException ex)
        {
            errorDetail = ex.Message;
            return false;
        }
        catch (OverflowException)
        {
            errorDetail = "JSON contém estrutura muito profunda ou complexa";
            return false;
        }
    }

    private void CountTokens(JsonElement element, ref int count)
    {
        count++;
        if (count > MaxTokenCount) return;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    CountTokens(property.Value, ref count);
                    if (count > MaxTokenCount) return;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CountTokens(item, ref count);
                    if (count > MaxTokenCount) return;
                }
                break;
        }
    }

    public bool IsTooLarge(string jsonText, int maxSizeInBytes = MaxJsonSizeInBytes)
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            return false;
        }

        int sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(jsonText);
        return sizeInBytes > maxSizeInBytes;
    }
}
using System.Text.Json;
using JSON2CSV.Client.Models;

namespace JSON2CSV.Client.Services;

public class JsonValidatorService : IJsonValidatorService
{
    private const int MaxJsonSizeInBytes = 10 * 1024 * 1024;

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

        if (!IsValidJson(jsonText))
        {
            return ConversionResult.Failure(
                "O JSON fornecido não é válido. Verifique a sintaxe.",
                ConversionErrorType.InvalidJson
            );
        }

        return ConversionResult.Success(string.Empty);
    }

    public bool IsEmpty(string jsonText)
    {
        return string.IsNullOrWhiteSpace(jsonText);
    }

    public bool IsValidJson(string jsonText)
    {
        try
        {
            JsonDocument.Parse(jsonText);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
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

using JSON2CSV.Client.Models;

namespace JSON2CSV.Client.Services;

public interface IJsonValidatorService
{
    ConversionResult Validate(string jsonText);

    bool IsEmpty(string jsonText);

    bool IsTooLarge(string jsonText, int maxSizeInBytes = 10 * 1024 * 1024);
}

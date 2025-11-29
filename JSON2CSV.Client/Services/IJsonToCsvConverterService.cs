using JSON2CSV.Client.Models;

namespace JSON2CSV.Client.Services;


public interface IJsonToCsvConverterService
{
    ConversionResult Convert(string jsonText);

    ConversionResult Convert(object? jsonObject);

    string EscapeCsvField(string value);

    string FormatValue(object? value);
}

namespace JSON2CSV.Client.Models;

public class ConversionResult
{
    public bool IsSuccess { get; set; }

    public string? CsvContent { get; set; }

    public string? ErrorMessage { get; set; }

    public ConversionErrorType ErrorType { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ConversionResult Success(string csvContent)
    {
        return new ConversionResult
        {
            IsSuccess = true,
            CsvContent = csvContent
        };
    }

    public static ConversionResult Failure(string errorMessage, ConversionErrorType errorType)
    {
        return new ConversionResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorType = errorType
        };
    }
}

public enum ConversionErrorType
{
    None,
    EmptyJson,
    InvalidJson,
    NestedStructureNotSupported,
    TooLarge,
    ConversionError
}

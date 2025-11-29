namespace JSON2CSV.Client.Models;

public class JsonInputModel
{
    public string JsonText { get; set; } = string.Empty;

    public bool IsValidFormat { get; set; }

    public object? ParsedObject { get; set; }

    public int SizeInBytes => string.IsNullOrEmpty(JsonText) ? 0 : System.Text.Encoding.UTF8.GetByteCount(JsonText);

    public bool IsEmpty => string.IsNullOrWhiteSpace(JsonText);
}

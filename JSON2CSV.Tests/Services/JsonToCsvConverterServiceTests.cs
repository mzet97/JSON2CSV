using JSON2CSV.Client.Services;
using JSON2CSV.Client.Models;
using Xunit;
using FluentAssertions;

namespace JSON2CSV.Tests.Services;

public class JsonToCsvConverterServiceTests
{
    private readonly JsonToCsvConverterService _converterService;

    public JsonToCsvConverterServiceTests()
    {
        _converterService = new JsonToCsvConverterService();
    }

    #region Array Conversion Tests

    [Fact]
    public void Convert_ValidJsonArrayOfObjects_ShouldReturnCsv()
    {
        // Arrange
        var jsonArray = @"[{""name"": ""John"", ""age"": 30}, {""name"": ""Jane"", ""age"": 25}]";

        // Act
        var result = _converterService.Convert(jsonArray);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().NotBeNullOrEmpty();
        result.CsvContent.Should().Contain("name");
        result.CsvContent.Should().Contain("John");
        result.CsvContent.Should().Contain("Jane");
    }

    [Fact]
    public void Convert_ArrayWithMissingProperties_ShouldHandleGracefully()
    {
        // Arrange
        var jsonArray = @"[{""name"": ""John"", ""age"": 30}, {""name"": ""Jane""}]";

        // Act
        var result = _converterService.Convert(jsonArray);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Convert_EmptyArray_ShouldReturnError()
    {
        // Arrange
        var emptyArray = @"[]";

        // Act
        var result = _converterService.Convert(emptyArray);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.InvalidJson);
    }

    #endregion

    #region Object Conversion Tests

    [Fact]
    public void Convert_SimpleJsonObject_ShouldReturnCsv()
    {
        // Arrange
        var jsonObject = @"{""name"": ""John"", ""age"": 30, ""city"": ""São Paulo""}";

        // Act
        var result = _converterService.Convert(jsonObject);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().NotBeNullOrEmpty();
        result.CsvContent.Should().Contain("name");
        result.CsvContent.Should().Contain("age");
        result.CsvContent.Should().Contain("city");
    }

    #endregion

    #region CSV Escaping Tests

    [Fact]
    public void Convert_JsonWithCommasInStrings_ShouldEscapeProperly()
    {
        // Arrange
        var jsonWithCommas = @"[{""name"": ""John, Jr."", ""city"": ""São Paulo, SP""}]";

        // Act
        var result = _converterService.Convert(jsonWithCommas);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("\"John, Jr.\"");
        result.CsvContent.Should().Contain("\"São Paulo, SP\"");
    }

    [Fact]
    public void Convert_JsonWithQuotesInStrings_ShouldEscapeProperly()
    {
        // Arrange
        var jsonWithQuotes = "[{\"name\": \"John \\\"The Doctor\\\" Smith\"}]";

        // Act
        var result = _converterService.Convert(jsonWithQuotes);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("\"John \"\"The Doctor\"\" Smith\"");
    }

    [Fact]
    public void Convert_JsonWithNewLinesInStrings_ShouldEscapeProperly()
    {
        // Arrange
        var jsonWithNewLines = @"[{""description"": ""First line\nSecond line""}]";

        // Act
        var result = _converterService.Convert(jsonWithNewLines);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("\"First line\nSecond line\"");
    }

    #endregion

    #region Data Type Tests

    [Fact]
    public void Convert_JsonWithDifferentDataTypes_ShouldConvertCorrectly()
    {
        // Arrange
        var jsonWithTypes = @"[{""name"": ""John"", ""age"": 30, ""active"": true, ""score"": 95.5, ""middleName"": null}]";

        // Act
        var result = _converterService.Convert(jsonWithTypes);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("John");
        result.CsvContent.Should().Contain("30");
        result.CsvContent.Should().Contain("true");
        result.CsvContent.Should().Contain("95.5");
    }

    [Fact]
    public void Convert_JsonWithNullValues_ShouldConvertToEmpty()
    {
        // Arrange
        var jsonWithNulls = @"[{""name"": ""John"", ""middleName"": null}]";

        // Act
        var result = _converterService.Convert(jsonWithNulls);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Convert_JsonWithSpecialCharacters_ShouldEscapeProperly()
    {
        // Arrange
        var jsonWithSpecial = @"[{""text"": ""Hello\tWorld"", ""currency"": ""R$ 100,00""}]";

        // Act
        var result = _converterService.Convert(jsonWithSpecial);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Convert_EmptyJson_ShouldReturnError()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = _converterService.Convert(emptyJson);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.EmptyJson);
    }

    [Fact]
    public void Convert_InvalidJson_ShouldReturnError()
    {
        // Arrange
        var invalidJson = @"{""name"": ""John""";

        // Act
        var result = _converterService.Convert(invalidJson);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.InvalidJson);
    }

    [Fact]
    public void Convert_JsonWithNestedObjects_ShouldExpandWithDotNotation()
    {
        // Arrange
        var nestedJson = @"{""name"": ""John"", ""address"": {""street"": ""Main St""}}";

        // Act
        var result = _converterService.Convert(nestedJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("address.street");
        result.CsvContent.Should().Contain("Main St");
        result.CsvContent.Should().Contain("John");
    }

    #endregion

    #region CSV Format Tests

    [Fact]
    public void Convert_ValidJsonArray_ShouldHaveProperCsvHeaders()
    {
        // Arrange
        var jsonArray = @"[{""id"": 1, ""name"": ""John"", ""age"": 30}]";

        // Act
        var result = _converterService.Convert(jsonArray);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var lines = result.CsvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCountGreaterOrEqualTo(2);
        lines[0].Should().Be("age,id,name");
    }

    [Fact]
    public void Convert_ArrayWithDifferentKeyOrders_ShouldMaintainHeaderOrder()
    {
        // Arrange
        var jsonArray = @"[{""name"": ""John"", ""age"": 30}, {""age"": 25, ""name"": ""Jane""}]";

        // Act
        var result = _converterService.Convert(jsonArray);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var lines = result.CsvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines[0].Should().Be("age,name");
    }

    #endregion

    #region AST Expansion Tests

    [Fact]
    public void Convert_ArrayOfPrimitives_ShouldJoinWithSemicolon()
    {
        // Arrange
        var jsonWithArray = @"[{""name"": ""Pedro"", ""hobbies"": [""futebol"", ""leitura"", ""viagens""]}]";

        // Act
        var result = _converterService.Convert(jsonWithArray);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("futebol; leitura; viagens");
    }

    [Fact]
    public void Convert_NestedObject_ShouldUseDotNotation()
    {
        // Arrange
        var jsonNested = @"[{""nome"": ""João"", ""endereco"": {""rua"": ""Paulista"", ""numero"": 1000}}]";

        // Act
        var result = _converterService.Convert(jsonNested);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("endereco.numero");
        result.CsvContent.Should().Contain("endereco.rua");
        result.CsvContent.Should().Contain("Paulista");
        result.CsvContent.Should().Contain("1000");
    }

    [Fact]
    public void Convert_ArrayOfObjects_ShouldExpandToMultipleRows()
    {
        // Arrange
        var jsonArrayOfObjects = @"[{""nome"": ""Carlos"", ""filhos"": [{""nome"": ""Lucas""}, {""nome"": ""Maria""}]}]";

        // Act
        var result = _converterService.Convert(jsonArrayOfObjects);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var lines = result.CsvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(3);
        result.CsvContent.Should().Contain("Lucas");
        result.CsvContent.Should().Contain("Maria");
    }

    [Fact]
    public void Convert_MultipleNestedArrays_ShouldCreateCartesianProduct()
    {
        // Arrange
        var jsonMultipleArrays = @"[{
                ""nome"": ""Ana"",
                ""telefones"": [{""numero"": ""1111""}, {""numero"": ""2222""}],
                ""emails"": [{""endereco"": ""ana1@email.com""}, {""endereco"": ""ana2@email.com""}]
            }]";

        // Act
        var result = _converterService.Convert(jsonMultipleArrays);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var lines = result.CsvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(5);
    }

    [Fact]
    public void Convert_DeeplyNested_ShouldFlattenCompletely()
    {
        // Arrange
        var deeplyNested = @"[{""a"": {""b"": {""c"": {""d"": ""value""}}}}]";

        // Act
        var result = _converterService.Convert(deeplyNested);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.CsvContent.Should().Contain("a.b.c.d");
        result.CsvContent.Should().Contain("value");
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void EscapeCsvField_StringWithoutSpecialCharacters_ShouldReturnAsIs()
    {
        // Act & Assert
        _converterService.EscapeCsvField("John").Should().Be("John");
        _converterService.EscapeCsvField("123").Should().Be("123");
    }

    [Fact]
    public void EscapeCsvField_StringWithComma_ShouldAddQuotes()
    {
        // Act & Assert
        _converterService.EscapeCsvField("São Paulo, SP").Should().Be("\"São Paulo, SP\"");
    }

    [Fact]
    public void EscapeCsvField_StringWithQuotes_ShouldEscapeThem()
    {
        // Act & Assert
        _converterService.EscapeCsvField("John \"The Doctor\" Smith").Should().Be("\"John \"\"The Doctor\"\" Smith\"");
    }

    [Fact]
    public void FormatValue_NullValue_ShouldReturnEmptyString()
    {
        // Act & Assert
        _converterService.FormatValue(null).Should().BeEmpty();
    }

    [Fact]
    public void FormatValue_DifferentTypes_ShouldFormatCorrectly()
    {
        // Act & Assert
        _converterService.FormatValue("John").Should().Be("John");
        _converterService.FormatValue(123).Should().Be("123");
        _converterService.FormatValue(true).Should().Be("true");
        _converterService.FormatValue(false).Should().Be("false");
    }

    #endregion
}

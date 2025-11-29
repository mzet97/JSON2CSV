using JSON2CSV.Client.Services;
using JSON2CSV.Client.Models;
using Xunit;
using FluentAssertions;

namespace JSON2CSV.Tests.Services;

public class JsonValidatorServiceTests
{
    private readonly JsonValidatorService _validatorService;

    public JsonValidatorServiceTests()
    {
        _validatorService = new JsonValidatorService();
    }

    #region Empty JSON Tests

    [Fact]
    public void Validate_EmptyJson_ShouldReturnEmptyJsonError()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = _validatorService.Validate(emptyJson);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.EmptyJson);
        result.ErrorMessage.Should().Contain("vazio");
    }

    [Fact]
    public void Validate_WhitespaceOnlyJson_ShouldReturnEmptyJsonError()
    {
        // Arrange
        var whitespaceJson = "   \n\t  ";

        // Act
        var result = _validatorService.Validate(whitespaceJson);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.EmptyJson);
    }

    #endregion

    #region Valid JSON Tests

    [Fact]
    public void Validate_ValidSimpleJson_ShouldReturnSuccess()
    {
        // Arrange
        var validJson = @"{""name"": ""John"", ""age"": 30}";

        // Act
        var result = _validatorService.Validate(validJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Validate_ValidJsonArray_ShouldReturnSuccess()
    {
        // Arrange
        var validArrayJson = @"[{""name"": ""John""}, {""name"": ""Jane""}]";

        // Act
        var result = _validatorService.Validate(validArrayJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidJsonWithDifferentTypes_ShouldReturnSuccess()
    {
        // Arrange
        var validJson = @"{""name"": ""John"", ""age"": 30, ""active"": true, ""score"": 95.5, ""middleName"": null}";

        // Act
        var result = _validatorService.Validate(validJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Invalid JSON Tests

    [Fact]
    public void Validate_InvalidJsonMalformed_ShouldReturnInvalidJsonError()
    {
        // Arrange
        var invalidJson = @"{""name"": ""John"", ""age"": 30";

        // Act
        var result = _validatorService.Validate(invalidJson);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.InvalidJson);
        result.ErrorMessage.Should().Contain("válido");
    }

    [Fact]
    public void Validate_InvalidJsonTrailingComma_ShouldReturnInvalidJsonError()
    {
        // Arrange
        var invalidJson = @"{""name"": ""John"", ""age"": 30,}";

        // Act
        var result = _validatorService.Validate(invalidJson);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.InvalidJson);
    }

    #endregion

    #region Nested Structure Tests (AST Support)

    [Fact]
    public void Validate_NestedObjectJson_ShouldReturnSuccess()
    {
        // Arrange
        var nestedJson = @"{""name"": ""John"", ""address"": {""street"": ""Main St""}}";

        // Act
        var result = _validatorService.Validate(nestedJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_NestedArrayJson_ShouldReturnSuccess()
    {
        // Arrange
        var nestedJson = @"{""name"": ""John"", ""hobbies"": [""reading"", ""gaming""]}";

        // Act
        var result = _validatorService.Validate(nestedJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_DeeplyNestedJson_ShouldReturnSuccess()
    {
        // Arrange
        var nestedJson = @"{""level1"": {""level2"": {""level3"": ""value""}}}";

        // Act
        var result = _validatorService.Validate(nestedJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_ArrayOfObjects_ShouldReturnSuccess()
    {
        // Arrange
        var arrayOfObjectsJson = @"[{""name"": ""John"", ""dependents"": [{""name"": ""Lucas""}]}]";

        // Act
        var result = _validatorService.Validate(arrayOfObjectsJson);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Size Tests

    [Fact]
    public void Validate_LargeJsonExceedingLimit_ShouldReturnTooLargeError()
    {
        // Arrange
        var largeJson = new string('x', 11 * 1024 * 1024); // 11MB

        // Act
        var result = _validatorService.Validate(largeJson);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ConversionErrorType.TooLarge);
        result.ErrorMessage.Should().Contain("grande");
    }

    #endregion

    #region Helper Methods Tests

    [Fact]
    public void IsEmpty_NullOrEmptyString_ShouldReturnTrue()
    {
        // Act & Assert
        _validatorService.IsEmpty(null).Should().BeTrue();
        _validatorService.IsEmpty("").Should().BeTrue();
        _validatorService.IsEmpty("   ").Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_ValidJsonString_ShouldReturnFalse()
    {
        // Act & Assert
        _validatorService.IsEmpty(@"{""name"": ""John""}").Should().BeFalse();
    }

    [Fact]
    public void IsValidJson_ValidJsonString_ShouldReturnTrue()
    {
        // Act & Assert
        var result = _validatorService.Validate(@"{""name"": ""John""}");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void IsValidJson_InvalidJsonString_ShouldReturnFalse()
    {
        // Act & Assert
        var result = _validatorService.Validate(@"{""name"": ""John""");
        result.IsSuccess.Should().BeFalse();
    }

    #endregion
}

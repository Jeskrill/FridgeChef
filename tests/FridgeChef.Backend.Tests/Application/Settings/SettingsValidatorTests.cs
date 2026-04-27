using FluentAssertions;
using FridgeChef.UserPreferences.Application.UseCases;

namespace FridgeChef.Backend.Tests.Application.Settings;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void UpdateDietsValidator_ShouldReturnValidationError_WhenTaxonIdsIsNull()
    {
        var validator = new UpdateDietsValidator();
        var request = new UpdateDietsRequest(null!);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateCuisinesValidator_ShouldReturnValidationError_WhenTaxonIdsIsNull()
    {
        var validator = new UpdateCuisinesValidator();
        var request = new UpdateCuisinesRequest(null!);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}

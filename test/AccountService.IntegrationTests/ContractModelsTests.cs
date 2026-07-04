using AccountService.Contract.Models;
using AccountService.Contract.Responses;
using FluentAssertions;

namespace AccountService.IntegrationTests;

public sealed class ContractModelsTests
{
    [Fact]
    public void Gender_ShouldContainExpectedValues()
    {
        Enum.GetNames<Gender>().Should().Contain(new[] { "Unspecified", "Male", "Female", "Other" });
    }

    [Fact]
    public void UserProfileResponse_ShouldStoreData()
    {
        var profile = new UserProfileResponse
        {
            UserId = Guid.NewGuid(),
            Username = "user",
            Gender = Gender.Other,
            Avatar = new byte[] { 1, 2, 3 },
        };

        profile.Username.Should().Be("user");
        profile.Gender.Should().Be(Gender.Other);
        profile.Avatar.Should().NotBeNull();
    }
}

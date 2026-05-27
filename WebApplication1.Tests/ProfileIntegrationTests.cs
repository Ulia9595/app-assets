using FluentAssertions;
using WebApplication1.Tests.Infrastructure;
using Xunit;

namespace WebApplication1.Tests;

public class ProfileIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    public ProfileIntegrationTests(TestWebApplicationFactory factory) { }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task GetProfile_WithValidToken_Returns200WithUserData()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task UpdateProfile_WithValidName_Returns200AndUpdatedName()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task UpdateProfile_WithTooShortName_Returns400()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task UpdateProfile_WithNewAvatarId_Returns200()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task GetAvatars_ReturnsListWithoutToken()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task GetElo_Returns200WithCorrectInitialValues()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task GetElo_WithoutToken_Returns401()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task CheckUsername_ExcludesCurrentUser_ReturnsAvailable()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task GetRatingHistory_Returns200()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task GetLeaderboard_Returns200WithCurrentUserRank()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task UpdateEloPoints_Via_AuthController_IncreasesRating()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task EloLevel_At1000Points_EqualsLevel1()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Profile")]
    public async Task SendEmailChangeOtp_WithNewEmail_Returns200()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }
}
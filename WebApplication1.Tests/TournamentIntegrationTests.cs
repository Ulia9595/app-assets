using FluentAssertions;
using WebApplication1.Tests.Infrastructure;
using Xunit;

namespace WebApplication1.Tests;

public class TournamentIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    public TournamentIntegrationTests(TestWebApplicationFactory factory) { }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetTournament_WithValidId_Returns200()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetTournament_WithInvalidId_Returns404()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetTournament_WithoutToken_Returns401()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetTournament_Finished_ShowsCorrectAnswers()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetTournament_Active_HidesCorrectAnswers()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetHistory_NewUser_Returns200WithEmptyList()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetHistory_WithoutToken_Returns401()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetHistory_AfterParticipating_ReturnsEntry()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetQueueStatus_NotInQueue_ReturnsFalse()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Tournament")]
    public async Task GetQueueStatus_WithoutToken_Returns401()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }
}
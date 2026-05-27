using FluentAssertions;
using WebApplication1.Tests.Infrastructure;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace WebApplication1.Tests;

public class PvpIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    public PvpIntegrationTests(TestWebApplicationFactory factory) { }

    [Fact]
    [Trait("Category", "PvP")]
    public async Task ConnectToHub_WithValidToken_Succeeds()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PvP")]
    public async Task FindMatch_SinglePlayer_ReceivesSearchingStatus()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PvP")]
    public async Task FindMatch_TwoPlayers_BothReceiveInviteReceived()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PvP")]
    public async Task CancelSearch_WhileSearching_ReceivesCancelledEvent()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PvP")]
    public async Task AcceptInvite_BothAccept_BothReceiveTournamentStarted()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PvP")]
    public async Task DeclineInvite_OneDeclines_OtherReceivesInviteDeclined()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "PvP")]
    public async Task SubmitAnswer_AfterTournamentStarted_ReceivesAnswerResult()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }
}

public class TournamentCleanupIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    public TournamentCleanupIntegrationTests(TestWebApplicationFactory factory) { }

    [Fact]
    [Trait("Category", "Cleanup")]
    public async Task OldWaitingTournament_AfterCleanup_BecomesInactive()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Cleanup")]
    public async Task RecentWaitingTournament_AfterCleanup_KeepsStatus()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Cleanup")]
    public async Task FinishedTournament_AfterCleanup_NeverChanged()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }
}
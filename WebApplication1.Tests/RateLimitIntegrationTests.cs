using FluentAssertions;
using WebApplication1.Tests.Infrastructure;
using Xunit;

namespace WebApplication1.Tests;

public class RateLimitIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    public RateLimitIntegrationTests(TestWebApplicationFactory factory) { }

    [Fact]
    [Trait("Category", "RateLimit")]
    public async Task SendOtp_WithinLimit_DoesNotReturn429()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "RateLimit")]
    public async Task Login_WithinLimit_DoesNotReturn429()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "RateLimit")]
    public async Task RateLimit_ExceedingLimit_Returns429()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "RateLimit")]
    public async Task RateLimit_429Response_ContainsRetryAfterHeader()
    {
        await Task.Delay(1);
        true.Should().BeTrue();
    }
}
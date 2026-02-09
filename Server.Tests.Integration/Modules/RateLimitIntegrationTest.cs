using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Server.Tests.Integration.TestHelpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApplication1.Models.Requests;
using Xunit;

namespace Server.Tests.Integration.Modules;

public class RateLimitIntegrationTest : IntegrationTestBase
{
    public RateLimitIntegrationTest(CustomWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Rate_Limiting_Should_Work_Across_Auth_And_Profile()
    {
        var email = $"rate.{Guid.NewGuid():N}@example.com";
        var password = "ValidPass123!";
        var name = "Rate Test User";

        var authResponse = await RegisterUserAsync(email, password, name);
        var token = authResponse.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var failedRequests = 0;
        for (int i = 0; i < 15; i++)
        {
            var eloResponse = await _client.PutAsync($"/api/auth/elo/{500 + i}", null);
            if (!eloResponse.IsSuccessStatusCode && eloResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                failedRequests++;
            }
            await Task.Delay(10);
        }

        failedRequests.Should().BeGreaterThan(0);

        var profileResponse = await _client.GetAsync("/api/profile");

        profileResponse.EnsureSuccessStatusCode();
    }
}
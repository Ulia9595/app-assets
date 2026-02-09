using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Server.Tests.Integration.TestHelpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApplication1.Models.Responses;
using Xunit;

namespace Server.Tests.Integration.Modules;

public class JwtIntegrationTest : IntegrationTestBase
{
    public JwtIntegrationTest(CustomWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task JWT_Token_Should_Work_Across_All_Authorized_Endpoints()
    {
        var email = $"jwt.{Guid.NewGuid():N}@example.com";
        var password = "ValidPass123!";
        var name = "JWT Test User";

        var authResponse = await RegisterUserAsync(email, password, name);
        var token = authResponse.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var validateResponse = await _client.GetAsync("/api/auth/validate");

        validateResponse.EnsureSuccessStatusCode();
        var validateResult = await validateResponse.Content.ReadFromJsonAsync<dynamic>();
        ((bool)validateResult!.isValid).Should().BeTrue();

        var profileResponse = await _client.GetAsync("/api/profile");

        profileResponse.EnsureSuccessStatusCode();
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        profileResult!.Data!.Email.Should().Be(email);

        var eloResponse = await _client.PutAsync($"/api/auth/elo/750", null);

        eloResponse.IsSuccessStatusCode.Should().BeTrue();

        var updatedProfileResponse = await _client.GetAsync("/api/profile");
        var updatedProfile = await updatedProfileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        updatedProfile!.Data!.EloPoints.Should().Be(750);
    }

    [Fact]
    public async Task Invalid_JWT_Should_Be_Rejected_By_Both_Controllers()
    {
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        var validateResponse = await _client.GetAsync("/api/auth/validate");
        validateResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var profileResponse = await _client.GetAsync("/api/profile");
        profileResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }
}
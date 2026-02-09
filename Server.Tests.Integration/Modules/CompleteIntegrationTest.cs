using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Server.Tests.Integration.TestHelpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;
using Xunit;

namespace Server.Tests.Integration.Modules;

public class CompleteIntegrationTest : IntegrationTestBase
{
    public CompleteIntegrationTest(CustomWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Complete_User_Journey_Should_Integrate_All_Requirements()
    {
        var email = $"user.{Guid.NewGuid():N}@example.com";
        var password = "ValidPass123!";
        var name = "Test User";

        var authResponse = await RegisterUserAsync(email, password, name);

        authResponse.Should().NotBeNull();
        authResponse.Token.Should().NotBeNullOrEmpty();
        authResponse.User.Email.Should().Be(email);
        authResponse.User.Name.Should().Be(name);

        _client.DefaultRequestHeaders.Authorization = null;
        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data!.Token.Should().NotBeNullOrEmpty();

        var token = loginResult.Data.Token;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var profileResponse = await _client.GetAsync("/api/profile");

        profileResponse.EnsureSuccessStatusCode();
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        profileResult!.Success.Should().BeTrue();
        profileResult.Data!.Email.Should().Be(email);
        profileResult.Data.Name.Should().Be(name);
        profileResult.Data.Uid.Should().Be(authResponse.User.Uid);

        var updateRequest = new UpdateProfileRequest
        {
            Name = "Updated Name",
            AvatarUrl = "https://example.com/new-avatar.jpg"
        };

        var updateResponse = await _client.PutAsJsonAsync("/api/profile", updateRequest);

        updateResponse.EnsureSuccessStatusCode();

        var updatedProfileResponse = await _client.GetAsync("/api/profile");
        var updatedProfile = await updatedProfileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        updatedProfile!.Data!.Name.Should().Be("Updated Name");
        updatedProfile.Data.AvatarUrl.Should().Be("https://example.com/new-avatar.jpg");

        await _client.PostAsync("/api/auth/logout", null);
        _client.DefaultRequestHeaders.Authorization = null;

        var newLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var newLoginResult = await newLoginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();

        newLoginResult!.Success.Should().BeTrue();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", newLoginResult.Data!.Token);

        var finalProfileResponse = await _client.GetAsync("/api/profile");
        var finalProfile = await finalProfileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        finalProfile!.Data!.Name.Should().Be("Updated Name");
        finalProfile.Data.Email.Should().Be(email);
    }
}
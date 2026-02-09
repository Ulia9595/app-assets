using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Server.Tests.Integration.TestHelpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApplication1.Models.Responses;
using Xunit;

namespace Server.Tests.Integration.Modules;

public class DataFlowTests : IntegrationTestBase
{
    public DataFlowTests(CustomWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task User_Data_Should_Flow_From_Auth_To_Profile_Correctly()
    {
        var email = $"flow.{Guid.NewGuid():N}@example.com";
        var password = "ValidPass123!";
        var name = "Data Flow User";
        var avatarUrl = "https://example.com/avatar1.jpg";

        var registerRequest = new
        {
            Email = email,
            Password = password,
            PasswordRepeat = password,
            Name = name,
            AvatarUrl = avatarUrl
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var authResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var token = authResult!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var profileResponse = await _client.GetAsync("/api/profile");
        profileResponse.EnsureSuccessStatusCode();

        var profileResult = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        profileResult!.Success.Should().BeTrue();
        profileResult.Data.Should().NotBeNull();

        profileResult.Data!.Email.Should().Be(email);
        profileResult.Data.Name.Should().Be(name);
        profileResult.Data.AvatarUrl.Should().Be(avatarUrl);
        profileResult.Data.Uid.Should().Be(authResult.Data.User.Uid);
        profileResult.Data.EloPoints.Should().Be(authResult.Data.User.EloPoints);
    }

    [Fact]
    public async Task Profile_Update_Should_Reflect_In_Subsequent_Auth_Operations()
    {
        var email = $"update.{Guid.NewGuid():N}@example.com";
        var password = "ValidPass123!";
        var name = "Original Name";

        var authResponse = await RegisterUserAsync(email, password, name);
        var token = authResponse.Token;

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var updatedName = "Updated User Name";
        var updatedAvatar = "https://example.com/new-avatar.jpg";

        var updateRequest = new { Name = updatedName, AvatarUrl = updatedAvatar };
        var updateResponse = await _client.PutAsJsonAsync("/api/profile", updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        var profileResponse = await _client.GetAsync("/api/profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        profile!.Data!.Name.Should().Be(updatedName);
        profile.Data.AvatarUrl.Should().Be(updatedAvatar);

        await _client.PostAsync("/api/auth/logout", null);
        _client.DefaultRequestHeaders.Authorization = null;

        var loginRequest = new { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var newAuthResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var newToken = newAuthResult!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        var finalProfileResponse = await _client.GetAsync("/api/profile");
        var finalProfile = await finalProfileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        finalProfile!.Data!.Name.Should().Be(updatedName);
        finalProfile.Data.AvatarUrl.Should().Be(updatedAvatar);
        finalProfile.Data.Email.Should().Be(email);
    }
}
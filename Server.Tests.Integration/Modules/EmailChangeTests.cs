using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Tests.Integration.TestHelpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApplication1.Data;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;
using Xunit;

namespace Server.Tests.Integration.Modules;

public class EmailChangeTests : IntegrationTestBase
{
    public EmailChangeTests(CustomWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task Email_Change_Should_Work_Across_Auth_And_Profile()
    {
        var oldEmail = $"old.{Guid.NewGuid():N}@example.com";
        var newEmail = $"new.{Guid.NewGuid():N}@example.com";
        var password = "ValidPass123!";
        var name = "Email Change User";

        var authResponse = await RegisterUserAsync(oldEmail, password, name);
        var token = authResponse.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var otpRequest = new { Email = newEmail };
        var otpResponse = await _client.PostAsJsonAsync("/api/auth/send-email-change-otp", otpRequest);

        otpResponse.IsSuccessStatusCode.Should().BeTrue();

        var otpCode = await _dbContext.OtpCodes
            .Where(o => o.Email == newEmail && o.Purpose == "email_change")
            .FirstOrDefaultAsync();

        otpCode.Should().NotBeNull();
        otpCode!.Used.Should().BeFalse();

        var verifyRequest = new { Email = newEmail, Code = otpCode.Code };
        var changeResponse = await _client.PostAsJsonAsync("/api/auth/change-email", verifyRequest);

        changeResponse.EnsureSuccessStatusCode();

        _client.DefaultRequestHeaders.Authorization = null;
        var oldLoginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = oldEmail, Password = password });

        oldLoginResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var newLoginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = newEmail, Password = password });

        newLoginResponse.EnsureSuccessStatusCode();
        var newAuthResult = await newLoginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        newAuthResult!.Success.Should().BeTrue();

        var newToken = newAuthResult.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        var profileResponse = await _client.GetAsync("/api/profile");

        profileResponse.EnsureSuccessStatusCode();
        var profileResult = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();

        profileResult!.Data!.Email.Should().Be(newEmail);
        profileResult.Data.Name.Should().Be(name);
    }
}
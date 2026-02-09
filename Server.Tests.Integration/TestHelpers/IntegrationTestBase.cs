using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Responses;
using Xunit;

namespace Server.Tests.Integration.TestHelpers;

public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly HttpClient _client;
    protected readonly CustomWebApplicationFactory _factory;
    protected readonly AppDbContext _dbContext;

    public IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();

        if (!_dbContext.AvailableAvatars.Any())
        {
            _dbContext.AvailableAvatars.AddRange(
                new AvailableAvatar { Category = "cats", Url = "https://example.com/avatar1.jpg", DisplayOrder = 1 },
                new AvailableAvatar { Category = "cats", Url = "https://example.com/avatar2.jpg", DisplayOrder = 2 },
                new AvailableAvatar { Category = "cats", Url = "https://example.com/avatar3.jpg", DisplayOrder = 3 }
            );

            _dbContext.SaveChanges();
        }
    }

    protected async Task<AuthResponse> RegisterUserAsync(string email, string password, string name)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            PasswordRepeat = password,
            Name = name,
            AvatarUrl = "https://example.com/avatar1.jpg"
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        return result?.Data!;
    }

    protected async Task<UserResponse> GetProfileAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/profile");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        return result?.Data!;
    }

    protected void ClearTracker()
    {
        _dbContext.ChangeTracker.Clear();
    }
}
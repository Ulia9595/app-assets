using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Responses;
using WebApplication1.Services;

namespace WebApplication1.Tests.Infrastructure
{
    public class NoOpMemoryCache : IMemoryCache
    {
        public bool TryGetValue(object key, out object? value)
        {
            value = null;
            return false;
        }

        public ICacheEntry CreateEntry(object key) => new NoOpCacheEntry(key);

        public void Remove(object key) { }

        public void Dispose() { }
    }

    public class NoOpCacheEntry : ICacheEntry
    {
        public NoOpCacheEntry(object key)
        {
            Key = key;
        }

        public object Key { get; }

        public object? Value { get; set; }

        public DateTimeOffset? AbsoluteExpiration { get; set; }

        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

        public TimeSpan? SlidingExpiration { get; set; }

        public IList<IChangeToken> ExpirationTokens { get; } =
            new List<IChangeToken>();

        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } =
            new List<PostEvictionCallbackRegistration>();

        public CacheItemPriority Priority { get; set; }

        public long? Size { get; set; }

        public void Dispose() { }
    }

    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName =
            $"TestDb_{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });

                var emailDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IEmailService));

                if (emailDescriptor != null)
                    services.Remove(emailDescriptor);

                var mockEmail = new Mock<IEmailService>();

                mockEmail.Setup(s =>
                        s.SendOtpEmail(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                    .ReturnsAsync(true);

                mockEmail.Setup(s =>
                        s.SendPasswordResetEmail(
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                    .ReturnsAsync(true);

                mockEmail.Setup(s =>
                        s.SendWelcomeEmail(
                            It.IsAny<string>(),
                            It.IsAny<string>()))
                    .ReturnsAsync(true);

                services.AddSingleton(mockEmail.Object);

                var cacheDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemoryCache));

                if (cacheDescriptor != null)
                    services.Remove(cacheDescriptor);

                services.AddSingleton<IMemoryCache, NoOpMemoryCache>();

                var hostedDescriptor = services.SingleOrDefault(
                    d => d.ImplementationType == typeof(TournamentCleanupService));

                if (hostedDescriptor != null)
                    services.Remove(hostedDescriptor);

                services.AddSingleton<TournamentCleanupService>();

                services.AddHostedService(sp =>
                    sp.GetRequiredService<TournamentCleanupService>());
            });
        }

        public async Task InitializeAsync()
        {
            using var scope = Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        public async Task SeedDatabaseAsync()
        {
            using var scope = Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.EnsureCreatedAsync();

            db.Users.RemoveRange(db.Users);
            db.UserRatings.RemoveRange(db.UserRatings);
            db.OtpCodes.RemoveRange(db.OtpCodes);

            await db.SaveChangesAsync();

            if (!db.Roles.Any())
            {
                db.Roles.AddRange(
                    new Role { Id = 1, Code = "player" },
                    new Role { Id = 2, Code = "admin" });

                await db.SaveChangesAsync();
            }

            if (!db.OtpPurposes.Any())
            {
                db.OtpPurposes.AddRange(
                    new OtpPurpose
                    {
                        Id = 1,
                        Name = "registration"
                    },
                    new OtpPurpose
                    {
                        Id = 2,
                        Name = "email_change"
                    },
                    new OtpPurpose
                    {
                        Id = 3,
                        Name = "password_reset"
                    });

                await db.SaveChangesAsync();
            }

            if (!db.AvailableAvatars.Any())
            {
                db.AvailableAvatars.AddRange(
                    new AvailableAvatar
                    {
                        Id = 1,
                        Url = "https://example.com/av1.png",
                        DisplayOrder = 1
                    },
                    new AvailableAvatar
                    {
                        Id = 2,
                        Url = "https://example.com/av2.png",
                        DisplayOrder = 2
                    });

                await db.SaveChangesAsync();
            }
        }

        public async Task SeedOtpCodeAsync(
            string email,
            string code,
            string purposeName = "registration")
        {
            using var scope = Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var purpose = await db.OtpPurposes
                .FirstOrDefaultAsync(p => p.Name == purposeName);

            if (purpose == null)
                return;

            var oldCodes = db.OtpCodes.Where(o =>
                o.Email == email &&
                o.PurposeId == purpose.Id);

            db.OtpCodes.RemoveRange(oldCodes);

            await db.SaveChangesAsync();

            db.OtpCodes.Add(new OtpCode
            {
                Email = email,
                Code = code,
                PurposeId = purpose.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                Used = false,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        public HttpClient CreateAuthenticatedClient(string token)
        {
            var client = CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            return client;
        }
    }

    public static class TestHelpers
    {
        private static readonly JsonSerializerOptions JsonOpts =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public static StringContent ToJson(object obj)
        {
            return new StringContent(
                JsonSerializer.Serialize(obj),
                Encoding.UTF8,
                "application/json");
        }

        public static async Task<T?> ReadAs<T>(
            HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }

        public static string UniqueEmail()
        {
            return $"test_{Guid.NewGuid():N}@yandex.ru";
        }

        public static string UniqueUsername()
        {
            return $"Usr_{Guid.NewGuid():N}"[..20];
        }

        public static async Task<string> RegisterAndGetToken(
    TestWebApplicationFactory factory,
    HttpClient client,
    string? email = null,
    string password = "TestPass123!")
        {
            email ??= UniqueEmail();

            var testCode = Random.Shared.Next(100000, 999999).ToString();

            await factory.SeedOtpCodeAsync(email, testCode);

            var verifyResp = await client.PostAsync("/api/auth/verify-otp",
                ToJson(new { email, code = testCode }));

            if (!verifyResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"VerifyOtp failed: {await verifyResp.Content.ReadAsStringAsync()}");
            }

            var uniqueName = "Usr" + Guid.NewGuid().ToString("N")[..20];

            var registerResp = await client.PostAsync("/api/auth/register",
                ToJson(new
                {
                    email,
                    password,
                    passwordRepeat = password,
                    name = uniqueName,
                    avatarId = 1
                }));

            if (!registerResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Register failed: {await registerResp.Content.ReadAsStringAsync()}");
            }

            var result = await ReadAs<ApiResponse<AuthResponse>>(registerResp);

            var token = result?.Data?.Token
                ?? throw new InvalidOperationException("Токен не получен");

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    var exists = await db.UserRatings
                        .AnyAsync(r => r.UserId == user.Id);

                    if (!exists)
                    {
                        db.UserRatings.Add(new UserRating
                        {
                            UserId = user.Id,
                            CurrentRating = 500,
                            LastUpdated = DateTime.UtcNow
                        });

                        await db.SaveChangesAsync();
                    }
                }
            }

            return token;
        }
    }
}
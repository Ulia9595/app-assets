using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebApplication1.Data;
using WebApplication1.Services;
using WebApplication1.Models.Entities;

namespace Server.Tests.Integration.TestHelpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
                options.EnableSensitiveDataLogging();
            });

            var emailServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));

            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }

            var mockEmailService = new Mock<IEmailService>();

            mockEmailService
                .Setup(x => x.SendOtpEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string email, string code, string purpose) =>
                {
                    Console.WriteLine($"MOCK: Отправка OTP {code} на {email} для {purpose}");
                    return true;
                });

            mockEmailService
                .Setup(x => x.SendPasswordResetEmail(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string email, string token) =>
                {
                    Console.WriteLine($"MOCK: Отправка reset token {token} на {email}");
                    return true;
                });

            mockEmailService
                .Setup(x => x.SendWelcomeEmail(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            services.AddSingleton(mockEmailService.Object);
        });
    }
}
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services
{
    public class TournamentCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<TournamentCleanupService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);
        private readonly TimeSpan _maxAge = TimeSpan.FromHours(1);
        private const int StatusWaiting = 1;
        private const int StatusInactive = 5;

        public TournamentCleanupService(
            IServiceProvider services,
            ILogger<TournamentCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TournamentCleanupService started");
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCleanupAsync();
                await Task.Delay(_interval, stoppingToken);
            }
        }

        // Публичный метод — вызывается напрямую из тестов
        public async Task RunCleanupAsync()
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                await using var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var cutoff = DateTime.UtcNow - _maxAge;

                var stale = await db.Tournaments
                    .Where(t =>
                        t.StatusId == StatusWaiting &&
                        t.CreatedAt < cutoff)
                    .ToListAsync();

                if (stale.Any())
                {
                    foreach (var t in stale)
                        t.StatusId = StatusInactive;

                    await db.SaveChangesAsync();

                    _logger.LogInformation(
                        "Cleanup: set {Count} tournaments to Inactive", stale.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TournamentCleanupService error");
            }
        }
    }
}
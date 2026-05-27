using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public class LeaderboardService
    {
        private readonly AppDbContext _context;

        public LeaderboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<LeaderboardResponse>> GetLeaderboardAsync(string currentUid)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Uid == currentUid);

            if (user == null)
                return ApiResponse<LeaderboardResponse>.Fail("Пользователь не найден");

            var entries = await _context.UserRatings
                .AsNoTracking()
                .Include(r => r.User)
                    .ThenInclude(u => u.Role)
                .Where(r => r.User.Role.Code != "admin")
                .OrderByDescending(r => r.CurrentRating)
                .Select(r => new LeaderboardEntryResponse
                {
                    UserId = r.UserId,
                    Name = r.User.Name ?? r.User.Email,
                    EloPoints = r.CurrentRating,
                    AvatarUrl = r.User.Avatar != null ? r.User.Avatar.Url : null
                })
                .ToListAsync();

            for (int i = 0; i < entries.Count; i++)
                entries[i].Rank = i + 1;

            var currentUserRank = entries.FirstOrDefault(e => e.UserId == user.Id)?.Rank ?? 0;

            return ApiResponse<LeaderboardResponse>.Ok(new LeaderboardResponse
            {
                Entries = entries,
                CurrentUserRank = currentUserRank
            });
        }
    }
}
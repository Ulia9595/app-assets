using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public class RatingHistoryService
    {
        private readonly AppDbContext _context;

        public RatingHistoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<RatingHistoryResponse>>> GetHistoryAsync(string uid, int page = 1, int pageSize = 20)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Uid == uid);

            if (user == null)
                return ApiResponse<List<RatingHistoryResponse>>.Fail("Пользователь не найден");

            var items = await _context.RatingHistories
                .AsNoTracking()
                .Where(h => h.UserId == user.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new RatingHistoryResponse
                {
                    Id = h.Id,
                    OldRating = h.OldRating,
                    NewRating = h.NewRating,
                    Delta = h.NewRating - h.OldRating,
                    Reason = h.Reason.Name,
                    TaskName = h.Task != null ? h.Task.Name : null,
                    TopicName = h.Topic != null ? h.Topic.Name : null,
                    TournamentId = h.TournamentId,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            return ApiResponse<List<RatingHistoryResponse>>.Ok(items);
        }
    }
}
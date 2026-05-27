using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public class ProfileService
    {
        private readonly AppDbContext _context;
        private readonly UsernameService _usernameService;

        public ProfileService(
            AppDbContext context,
            UsernameService usernameService)
        {
            _context = context;
            _usernameService = usernameService;
        }

        public async Task<ApiResponse<UserResponse>> GetProfileAsync(string uid)
        {
            Console.WriteLine($"GetProfileAsync вызван для UID: {uid}");

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Avatar)
                .Include(u => u.Rating)
                .FirstOrDefaultAsync(u => u.Uid == uid);

            if (user == null)
            {
                Console.WriteLine($"Пользователь с UID {uid} не найден");
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            var response = new UserResponse
            {
                Uid = user.Uid!,
                Email = user.Email,
                Role = user.Role.Code,
                Name = user.Name,
                AvatarId = user.AvatarId,
                AvatarUrl = user.Avatar?.Url,
                EloPoints = user.Rating?.CurrentRating ?? 500
            };

            return ApiResponse<UserResponse>.Ok(response);
        }

        public async Task<User?> GetUserById(int userId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Avatar)
                .Include(u => u.Rating)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ApiResponse<UserResponse>> UpdateProfileAsync(string uid, UpdateProfileRequest request)
        {
            Console.WriteLine($"UpdateProfileAsync вызван для UID={uid}");
            Console.WriteLine($"Новое имя: {request.Name}, Новый AvatarId: {request.AvatarId}");

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Avatar)
                .Include(u => u.Rating)
                .FirstOrDefaultAsync(u => u.Uid == uid);

            if (user == null)
            {
                Console.WriteLine($"Пользователь с UID {uid} не найден");
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            if (user.Role.Code == "admin")
                return ApiResponse<UserResponse>.Fail("Администратор не может изменять профиль");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var usernameCheck = await _usernameService.ValidateAndCheckUsernameAsync(
                    request.Name,
                    currentUserId: uid
                );

                if (!usernameCheck.Success)
                    return ApiResponse<UserResponse>.Fail(usernameCheck.Error ?? "Некорректное имя пользователя");

                user.Name = request.Name.Trim();
            }

            if (request.AvatarId.HasValue)
            {
                var avatarExists = await _context.AvailableAvatars
                    .AnyAsync(a => a.Id == request.AvatarId.Value);

                if (!avatarExists)
                    return ApiResponse<UserResponse>.Fail("Выбранный аватар не найден");

                user.AvatarId = request.AvatarId.Value;
            }

            Console.WriteLine("Сохраняем изменения в БД...");
            await _context.SaveChangesAsync();

            Console.WriteLine($"Обновление профиля завершено для UID={uid}");
            return await GetProfileAsync(uid);
        }

        public async Task<ApiResponse<List<AvatarResponse>>> GetAvailableAvatarsAsync()
        {
            try
            {
                var avatars = await _context.AvailableAvatars
                    .OrderBy(a => a.DisplayOrder)
                    .Select(a => new AvatarResponse
                    {
                        Id = a.Id,
                        Url = a.Url,
                        DisplayOrder = a.DisplayOrder
                    })
                    .ToListAsync();

                return ApiResponse<List<AvatarResponse>>.Ok(avatars);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка EF при получении аватаров: {ex.Message}");
                return ApiResponse<List<AvatarResponse>>.Fail("Ошибка получения списка аватаров");
            }
        }

        public async Task<ApiResponse<UserResponse>> CompleteRegistrationAsync(string uid, CompleteRegistrationRequest request)
        {
            Console.WriteLine($"CompleteRegistrationAsync вызван для UID={uid}");
            Console.WriteLine($"Имя: {request.Name}, AvatarId: {request.AvatarId}");

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Avatar)
                .Include(u => u.Rating)
                .FirstOrDefaultAsync(u => u.Uid == uid);

            if (user == null)
            {
                Console.WriteLine($"Пользователь с UID {uid} не найден");
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            if (user.Role.Code == "admin")
                return ApiResponse<UserResponse>.Fail("Администратор не проходит завершение регистрации игрока");

            var usernameCheck = await _usernameService.ValidateAndCheckUsernameAsync(
                request.Name,
                currentUserId: uid
            );

            if (!usernameCheck.Success)
                return ApiResponse<UserResponse>.Fail(usernameCheck.Error ?? "Некорректное имя пользователя");

            user.Name = request.Name.Trim();

            if (request.AvatarId.HasValue)
            {
                var avatarExists = await _context.AvailableAvatars
                    .AnyAsync(a => a.Id == request.AvatarId.Value);

                if (!avatarExists)
                    return ApiResponse<UserResponse>.Fail("Выбранный аватар не найден");

                user.AvatarId = request.AvatarId.Value;
            }

            Console.WriteLine("Сохраняем изменения в БД...");
            await _context.SaveChangesAsync();

            Console.WriteLine($"Завершение регистрации завершено для UID={uid}");
            return await GetProfileAsync(uid);
        }

        public async Task<ApiResponse<bool>> IsUsernameUniqueAsync(string username, string? currentUid)
        {
            return await _usernameService.IsUsernameUniqueAsync(username, currentUid);
        }

        public async Task<ApiResponse<bool>> CheckUsernameAsync(string username, string? currentUid)
        {
            return await _usernameService.ValidateAndCheckUsernameAsync(username, currentUid);
        }
    }
}
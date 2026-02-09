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

        public ProfileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<UserResponse>> GetProfileAsync(string uid)
        {
            Console.WriteLine($"GetProfileAsync вызван для UID: {uid}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Uid == uid);

            if (user == null)
            {
                Console.WriteLine($"Пользователь с UID {uid} не найден");
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            Console.WriteLine($"Пользователь найден:");
            Console.WriteLine($"Имя: '{user.Name}'");
            Console.WriteLine($"Аватар: '{user.AvatarUrl}'");
            Console.WriteLine($"Email: '{user.Email}'");
            Console.WriteLine($"ELO: {user.EloPoints}");

            var response = new UserResponse
            {
                Uid = user.Uid!,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                EloPoints = user.EloPoints,
                IsEmailVerified = user.IsEmailVerified
            };

            return ApiResponse<UserResponse>.Ok(response);
        }

        public async Task<User?> GetUserById(int userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<ApiResponse<UserResponse>> UpdateProfileAsync(string uid, UpdateProfileRequest request)
        {
            Console.WriteLine($"UpdateProfileAsync вызван для UID={uid}");
            Console.WriteLine($"Новое имя: {request.Name}, Новый аватар: {request.AvatarUrl}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Uid == uid);

            if (user == null)
            {
                Console.WriteLine($"Пользователь с UID {uid} не найден");
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            if (!string.IsNullOrEmpty(request.Name))
                user.Name = request.Name;

            if (!string.IsNullOrEmpty(request.AvatarUrl))
                user.AvatarUrl = request.AvatarUrl;

            user.UpdatedAt = DateTime.UtcNow;
            Console.WriteLine($"Сохраняем изменения в БД...");
            await _context.SaveChangesAsync();

            Console.WriteLine($"Обновление профиля завершено для UID={uid}");
            return await GetProfileAsync(uid);
        }

        public async Task<ApiResponse<List<string>>> GetAvailableAvatarsAsync()
        {
            try
            {
                var avatars = await _context.AvailableAvatars
                    .OrderBy(a => a.DisplayOrder)
                    .Select(a => a.Url)
                    .ToListAsync();

                if (avatars.Any())
                {
                    return ApiResponse<List<string>>.Ok(avatars);
                }

                return ApiResponse<List<string>>.Ok(GetDefaultAvatars());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка EF при получении аватаров: {ex.Message}");
                return ApiResponse<List<string>>.Ok(GetDefaultAvatars());
            }
        }

        private List<string> GetDefaultAvatars()
        {
            return new List<string>
            {
                "https://raw.githubusercontent.com/Ulia9595/app-assets/main/avatars/Cat_1.jpg",
                "https://github.com/Ulia9595/app-assets/blob/main/avatars/Cat_2.jpg?raw=true",
                "https://github.com/Ulia9595/app-assets/blob/main/avatars/Cat_3.jpg?raw=true",
                "https://github.com/Ulia9595/app-assets/blob/main/avatars/Cat_4.jpg?raw=true",
                "https://github.com/Ulia9595/app-assets/blob/main/avatars/Cat_5.jpg?raw=true",
                "https://github.com/Ulia9595/app-assets/blob/main/avatars/Cat_6.jpg?raw=true"
            };
        }

        public async Task<ApiResponse<UserResponse>> CompleteRegistrationAsync(string uid, CompleteRegistrationRequest request)
        {
            Console.WriteLine($"CompleteRegistrationAsync вызван для UID={uid}");
            Console.WriteLine($"Имя: {request.Name}, Аватар: {request.AvatarUrl}");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Uid == uid);

            if (user == null)
            {
                Console.WriteLine($"Пользователь с UID {uid} не найден");
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            user.Name = request.Name;
            user.AvatarUrl = request.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;

            Console.WriteLine($"Сохраняем изменения в БД...");
            await _context.SaveChangesAsync();

            Console.WriteLine($"Завершение регистрации завершено для UID={uid}");
            return await GetProfileAsync(uid);
        }

        public async Task<ApiResponse<bool>> IsUsernameUniqueAsync(string username, string? currentUid)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ApiResponse<bool>.Fail("Имя не может быть пустым");

            var exists = await _context.Users.AnyAsync(u =>
                u.Name != null &&
                u.Name.ToLower() == username.ToLower() &&
                (currentUid == null || u.Uid != currentUid)
            );

            return ApiResponse<bool>.Ok(!exists);
        }
    }
}
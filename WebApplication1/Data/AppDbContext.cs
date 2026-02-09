using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Entities;

namespace WebApplication1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<AvailableAvatar> AvailableAvatars { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<PasswordResetAttempt> PasswordResetAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Uid).HasColumnName("uid");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
                entity.Property(e => e.EloPoints).HasColumnName("elo_points");
                entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<OtpCode>(entity =>
            {
                entity.ToTable("otp_codes");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Purpose).HasColumnName("purpose");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.Used).HasColumnName("used");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("password_reset_tokens");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Token).HasColumnName("token");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.Used).HasColumnName("used");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<PasswordResetAttempt>(entity =>
            {
                entity.ToTable("password_reset_attempts");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.IpAddress).HasColumnName("ip_address");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.Blocked).HasColumnName("blocked");
                entity.Property(e => e.BlockReason).HasColumnName("block_reason");
                entity.Property(e => e.UserAgent).HasColumnName("user_agent");

                entity.HasIndex(e => new { e.Email, e.CreatedAt });
            });

            modelBuilder.Entity<UserRating>(entity =>
            {
                entity.ToTable("user_ratings");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.CurrentRating).HasColumnName("current_rating");
                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.GamesPlayed).HasColumnName("games_played");
                entity.Property(e => e.GamesWon).HasColumnName("games_won");
                entity.Property(e => e.GamesLost).HasColumnName("games_lost");
                entity.Property(e => e.WinStreak).HasColumnName("win_streak");
                entity.Property(e => e.BestRating).HasColumnName("best_rating");
                entity.Property(e => e.LastUpdated).HasColumnName("last_updated");
            });

            modelBuilder.Entity<AvailableAvatar>(entity =>
            {
                entity.ToTable("available_avatars");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Category).HasColumnName("category").HasDefaultValue("cats");
                entity.Property(e => e.Url).HasColumnName("url");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Uid)
                .IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<UserRating>()
                .HasIndex(r => r.UserId)
                .IsUnique();
        }
    }
}
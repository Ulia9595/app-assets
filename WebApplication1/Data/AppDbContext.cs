using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Entities;

namespace WebApplication1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<AvailableAvatar> AvailableAvatars { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<PasswordResetAttempt> PasswordResetAttempts { get; set; }
        public DbSet<OtpPurpose> OtpPurposes { get; set; }
        public DbSet<RatingChangeReason> RatingChangeReasons { get; set; }
        public DbSet<RatingHistory> RatingHistories { get; set; }

        public DbSet<Topic> Topics { get; set; }
        public DbSet<Level> Levels { get; set; }
        public DbSet<Theory> Theories { get; set; }
        public DbSet<DifficultyType> DifficultyTypes { get; set; }
        public DbSet<CheckType> CheckTypes { get; set; }
        public DbSet<PracticeTask> PracticeTasks { get; set; }
        public DbSet<CustomCheckAlgorithm> CustomCheckAlgorithms { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<Hint> Hints { get; set; }
        public DbSet<Solution> Solutions { get; set; }

        public DbSet<TournamentStatus> TournamentStatuses { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<TournamentQuestion> TournamentQuestions { get; set; }
        public DbSet<PlayerAnswer> PlayerAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Code)
                    .HasColumnName("code")
                    .HasMaxLength(50)
                    .IsRequired();
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Uid).HasColumnName("uid").HasMaxLength(255);
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(30);
                entity.Property(e => e.AvatarId).HasColumnName("avatar_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Avatar)
                    .WithMany(a => a.Users)
                    .HasForeignKey(e => e.AvatarId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Rating)
                    .WithOne(r => r.User)
                    .HasForeignKey<UserRating>(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Uid).IsUnique();
                entity.HasIndex(e => e.RoleId);
                entity.HasIndex(e => e.AvatarId);
            });

            modelBuilder.Entity<OtpCode>(entity =>
            {
                entity.ToTable("otp_codes");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId)
                    .HasColumnName("user_id");
                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(e => e.Code)
                    .HasColumnName("code")
                    .HasMaxLength(6)
                    .IsRequired();
                entity.Property(e => e.PurposeId)
                    .HasColumnName("purpose_id")
                    .IsRequired();
                entity.Property(e => e.ExpiresAt)
                    .HasColumnName("expires_at");
                entity.Property(e => e.Used)
                    .HasColumnName("used")
                    .HasDefaultValue(false);
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Purpose)
                    .WithMany(p => p.OtpCodes)
                    .HasForeignKey(e => e.PurposeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.Email, e.Code });
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.PurposeId);
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
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Token).IsUnique();
            });

            modelBuilder.Entity<PasswordResetAttempt>(entity =>
            {
                entity.ToTable("password_reset_attempts");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.IpAddress).HasColumnName("ip_address");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.HasIndex(e => new { e.Email, e.CreatedAt });
            });

            modelBuilder.Entity<UserRating>(entity =>
            {
                entity.ToTable("user_ratings");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();
                entity.Property(e => e.CurrentRating)
                    .HasColumnName("current_rating")
                    .HasDefaultValue(500);
                entity.Property(e => e.LastUpdated)
                    .HasColumnName("last_updated")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.CurrentRating);
            });

            modelBuilder.Entity<AvailableAvatar>(entity =>
            {
                entity.ToTable("available_avatars");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Url)
                    .HasColumnName("url")
                    .IsRequired();
                entity.Property(e => e.DisplayOrder)
                    .HasColumnName("display_order")
                    .HasDefaultValue(0);
                entity.HasIndex(e => e.DisplayOrder);
            });

            modelBuilder.Entity<OtpPurpose>(entity =>
            {
                entity.ToTable("otp_purposes");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasData(
                    new OtpPurpose { Id = 1, Name = "registration" },
                    new OtpPurpose { Id = 2, Name = "email_change" },
                    new OtpPurpose { Id = 3, Name = "password_reset" }
                );
            });

            modelBuilder.Entity<RatingChangeReason>(entity =>
            {
                entity.ToTable("rating_change_reasons");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<RatingHistory>(entity =>
            {
                entity.ToTable("rating_history");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.OldRating).HasColumnName("old_rating");
                entity.Property(e => e.NewRating).HasColumnName("new_rating");
                entity.Property(e => e.ReasonId).HasColumnName("reason_id");
                entity.Property(e => e.TaskId).HasColumnName("task_id");
                entity.Property(e => e.TopicId).HasColumnName("topic_id");
                entity.Property(e => e.TournamentId).HasColumnName("tournament_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Reason).WithMany(r => r.RatingHistories).HasForeignKey(e => e.ReasonId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Task).WithMany(t => t.RatingHistories).HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Topic).WithMany(t => t.RatingHistories).HasForeignKey(e => e.TopicId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Tournament).WithMany(t => t.RatingHistories).HasForeignKey(e => e.TournamentId).OnDelete(DeleteBehavior.SetNull);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ReasonId);
                entity.HasIndex(e => e.TaskId);
                entity.HasIndex(e => e.TopicId);
                entity.HasIndex(e => e.TournamentId);
            });

            modelBuilder.Entity<Topic>(entity =>
            {
                entity.ToTable("topics");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
                entity.HasIndex(e => e.DisplayOrder);
            });

            modelBuilder.Entity<Level>(entity =>
            {
                entity.ToTable("levels");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TopicId).HasColumnName("topic_id").IsRequired();
                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(255)
                    .IsRequired();
                entity.Property(e => e.LevelNumber).HasColumnName("level_number");
                entity.HasOne(e => e.Topic)
                    .WithMany(t => t.Levels)
                    .HasForeignKey(e => e.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Theory)
                    .WithOne(t => t.Level)
                    .HasForeignKey<Theory>(t => t.LevelId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.TopicId);
                entity.HasIndex(e => new { e.TopicId, e.LevelNumber }).IsUnique();
            });

            modelBuilder.Entity<Theory>(entity =>
            {
                entity.ToTable("theories");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.LevelId).HasColumnName("level_id").IsRequired();
                entity.Property(e => e.Title)
                    .HasColumnName("title")
                    .HasMaxLength(500)
                    .IsRequired();
                entity.Property(e => e.Content).HasColumnName("content").IsRequired();
                entity.HasIndex(e => e.LevelId).IsUnique();
            });

            modelBuilder.Entity<DifficultyType>(entity =>
            {
                entity.ToTable("difficulty_types");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<CheckType>(entity =>
            {
                entity.ToTable("check_types");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<PracticeTask>(entity =>
            {
                entity.ToTable("practice_tasks");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.LevelId).HasColumnName("level_id").IsRequired();
                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(500)
                    .IsRequired();
                entity.Property(e => e.Condition).HasColumnName("condition").IsRequired();
                entity.Property(e => e.DifficultyTypeId).HasColumnName("difficulty_type_id").IsRequired();
                entity.Property(e => e.DisplayOrder)
                    .HasColumnName("display_order")
                    .HasDefaultValue(0);
                entity.Property(e => e.CheckTypeId).HasColumnName("check_type_id").IsRequired();
                entity.HasOne(e => e.Level)
                    .WithMany(l => l.PracticeTasks)
                    .HasForeignKey(e => e.LevelId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.DifficultyType)
                    .WithMany(d => d.PracticeTasks)
                    .HasForeignKey(e => e.DifficultyTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CheckType)
                    .WithMany(c => c.PracticeTasks)
                    .HasForeignKey(e => e.CheckTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CustomCheckAlgorithm)
                    .WithOne(a => a.PracticeTask)
                    .HasForeignKey<CustomCheckAlgorithm>(a => a.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.LevelId);
                entity.HasIndex(e => e.DifficultyTypeId);
                entity.HasIndex(e => e.CheckTypeId);
            });

            modelBuilder.Entity<CustomCheckAlgorithm>(entity =>
            {
                entity.ToTable("custom_check_algorithms");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TaskId).HasColumnName("task_id").IsRequired();
                entity.Property(e => e.AlgorithmCode).HasColumnName("algorithm_code").IsRequired();
                entity.HasIndex(e => e.TaskId).IsUnique();
            });

            modelBuilder.Entity<TestCase>(entity =>
            {
                entity.ToTable("test_cases");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TaskId).HasColumnName("task_id").IsRequired();
                entity.Property(e => e.InputData).HasColumnName("input_data");
                entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output").IsRequired();
                entity.Property(e => e.IsHidden)
                    .HasColumnName("is_hidden")
                    .HasDefaultValue(false);
                entity.HasOne(e => e.PracticeTask)
                    .WithMany(t => t.TestCases)
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.TaskId);
            });

            modelBuilder.Entity<Hint>(entity =>
            {
                entity.ToTable("hints");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TaskId).HasColumnName("task_id").IsRequired();
                entity.Property(e => e.HintText).HasColumnName("hint_text").IsRequired();
                entity.Property(e => e.DisplayOrder)
                    .HasColumnName("display_order")
                    .HasDefaultValue(0);
                entity.HasOne(e => e.PracticeTask)
                    .WithMany(t => t.Hints)
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.TaskId);
                entity.HasIndex(e => new { e.TaskId, e.DisplayOrder });
            });

            modelBuilder.Entity<Solution>(entity =>
            {
                entity.ToTable("solutions");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.TaskId).HasColumnName("task_id").IsRequired();
                entity.Property(e => e.SolutionCode).HasColumnName("solution_code").IsRequired();
                entity.Property(e => e.IsCorrect).HasColumnName("is_correct").HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Task).WithMany(t => t.Solutions).HasForeignKey(e => e.TaskId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TaskId);
                entity.HasIndex(e => new { e.UserId, e.TaskId });
            });

            modelBuilder.Entity<TournamentStatus>(entity =>
            {
                entity.ToTable("tournament_statuses");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasData(
                    new TournamentStatus { Id = 1, Name = "Ожидание соперника" },
                    new TournamentStatus { Id = 2, Name = "В процессе" },
                    new TournamentStatus { Id = 3, Name = "Завершён" },
                    new TournamentStatus { Id = 4, Name = "Отменён" }
                );
            });

            modelBuilder.Entity<Tournament>(entity =>
            {
                entity.ToTable("tournaments");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TopicId).HasColumnName("topic_id").IsRequired();
                entity.Property(e => e.MinRating).HasColumnName("min_rating").HasDefaultValue(0);
                entity.Property(e => e.MaxRating).HasColumnName("max_rating").HasDefaultValue(9999);
                entity.Property(e => e.StatusId).HasColumnName("status_id").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.Topic).WithMany(t => t.Tournaments).HasForeignKey(e => e.TopicId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Status).WithMany(s => s.Tournaments).HasForeignKey(e => e.StatusId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.TopicId);
                entity.HasIndex(e => e.StatusId);
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("questions");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TopicId).HasColumnName("topic_id").IsRequired();
                entity.Property(e => e.DifficultyTypeId).HasColumnName("difficulty_type_id").IsRequired();
                entity.Property(e => e.QuestionText).HasColumnName("question_text").IsRequired();
                entity.HasOne(e => e.Topic).WithMany(t => t.Questions).HasForeignKey(e => e.TopicId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.DifficultyType).WithMany().HasForeignKey(e => e.DifficultyTypeId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => e.TopicId);
                entity.HasIndex(e => e.DifficultyTypeId);
            });

            modelBuilder.Entity<AnswerOption>(entity =>
            {
                entity.ToTable("answer_options");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.QuestionId).HasColumnName("question_id").IsRequired();
                entity.Property(e => e.AnswerText).HasColumnName("answer_text").IsRequired();
                entity.Property(e => e.IsCorrect).HasColumnName("is_correct").HasDefaultValue(false);
                entity.HasOne(e => e.Question).WithMany(q => q.AnswerOptions).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.QuestionId);
            });

            modelBuilder.Entity<TournamentQuestion>(entity =>
            {
                entity.ToTable("tournament_questions");

                entity.HasKey(e => new { e.TournamentId, e.QuestionId });
                entity.Property(e => e.TournamentId).HasColumnName("tournament_id");
                entity.Property(e => e.QuestionId).HasColumnName("question_id");
                entity.Property(e => e.QuestionNumber).HasColumnName("question_number").IsRequired();
                entity.HasOne(e => e.Tournament).WithMany(t => t.TournamentQuestions).HasForeignKey(e => e.TournamentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Question).WithMany(q => q.TournamentQuestions).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.TournamentId, e.QuestionNumber }).IsUnique();
                entity.HasIndex(e => e.QuestionId);
            });

            modelBuilder.Entity<PlayerAnswer>(entity =>
            {
                entity.ToTable("player_answers");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.QuestionId).HasColumnName("question_id").IsRequired();
                entity.Property(e => e.TournamentId).HasColumnName("tournament_id").IsRequired();
                entity.Property(e => e.AnswerOptionId).HasColumnName("answer_option_id").IsRequired();
                entity.Property(e => e.AnsweredAt).HasColumnName("answered_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Question).WithMany(q => q.PlayerAnswers).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Tournament).WithMany(t => t.PlayerAnswers).HasForeignKey(e => e.TournamentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.AnswerOption).WithMany(a => a.PlayerAnswers).HasForeignKey(e => e.AnswerOptionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.UserId, e.QuestionId, e.TournamentId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TournamentId);
                entity.HasIndex(e => e.QuestionId);
            });
        }
    }
}
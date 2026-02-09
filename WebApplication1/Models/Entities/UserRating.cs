using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("user_ratings")]
    public class UserRating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("current_rating")]
        public int CurrentRating { get; set; } = 500;

        [Column("level")]
        public int Level { get; set; } = 0;

        [Column("games_played")]
        public int GamesPlayed { get; set; } = 0;

        [Column("games_won")]
        public int GamesWon { get; set; } = 0;

        [Column("games_lost")]
        public int GamesLost { get; set; } = 0;

        [Column("win_streak")]
        public int WinStreak { get; set; } = 0;

        [Column("best_rating")]
        public int BestRating { get; set; } = 500;

        [Column("last_updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("tournaments")]
    public class Tournament
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("topic_id")]
        public int TopicId { get; set; }

        [Column("min_rating")]
        public int MinRating { get; set; } = 0;

        [Column("max_rating")]
        public int MaxRating { get; set; } = 9999;

        [Required]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(TopicId))]
        public Topic Topic { get; set; } = null!;

        [ForeignKey(nameof(StatusId))]
        public TournamentStatus Status { get; set; } = null!;

        public ICollection<TournamentQuestion> TournamentQuestions { get; set; } = new List<TournamentQuestion>();
        public ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
        public ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();
    }
}

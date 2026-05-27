using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("rating_history")]
    public class RatingHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("old_rating")]
        public int OldRating { get; set; }

        [Column("new_rating")]
        public int NewRating { get; set; }

        [Column("reason_id")]
        public int ReasonId { get; set; }

        [Column("task_id")]
        public int? TaskId { get; set; }

        [Column("topic_id")]
        public int? TopicId { get; set; }

        [Column("tournament_id")]
        public int? TournamentId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(ReasonId))]
        public RatingChangeReason Reason { get; set; } = null!;

        [ForeignKey(nameof(TaskId))]
        public PracticeTask? Task { get; set; }

        [ForeignKey(nameof(TopicId))]
        public Topic? Topic { get; set; }

        [ForeignKey(nameof(TournamentId))]
        public Tournament? Tournament { get; set; }
    }
}
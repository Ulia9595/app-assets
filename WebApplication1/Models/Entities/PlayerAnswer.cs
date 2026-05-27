using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{

    [Table("player_answers")]
    public class PlayerAnswer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("tournament_id")]
        public int TournamentId { get; set; }

        [Required]
        [Column("answer_option_id")]
        public int AnswerOptionId { get; set; }

        [Column("answered_at")]
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = null!;

        [ForeignKey(nameof(TournamentId))]
        public Tournament Tournament { get; set; } = null!;

        [ForeignKey(nameof(AnswerOptionId))]
        public AnswerOption AnswerOption { get; set; } = null!;
    }
}

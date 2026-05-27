using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("tournament_questions")]
    public class TournamentQuestion
    {
        [Column("tournament_id")]
        public int TournamentId { get; set; }

        [Column("question_id")]
        public int QuestionId { get; set; }

        [Required]
        [Column("question_number")]
        public int QuestionNumber { get; set; }

        [ForeignKey(nameof(TournamentId))]
        public Tournament Tournament { get; set; } = null!;

        [ForeignKey(nameof(QuestionId))]
        public Question Question { get; set; } = null!;
    }
}

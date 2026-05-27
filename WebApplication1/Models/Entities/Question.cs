using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("questions")]
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("topic_id")]
        public int TopicId { get; set; }

        [Required]
        [Column("difficulty_type_id")]
        public int DifficultyTypeId { get; set; }

        [Required]
        [Column("question_text")]
        public string QuestionText { get; set; } = string.Empty;

        [ForeignKey(nameof(TopicId))]
        public Topic Topic { get; set; } = null!;

        [ForeignKey(nameof(DifficultyTypeId))]
        public DifficultyType DifficultyType { get; set; } = null!;

        public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
        public ICollection<TournamentQuestion> TournamentQuestions { get; set; } = new List<TournamentQuestion>();
        public ICollection<PlayerAnswer> PlayerAnswers { get; set; } = new List<PlayerAnswer>();
    }
}

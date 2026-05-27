using Org.BouncyCastle.Tsp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("practice_tasks")]
    public class PracticeTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("level_id")]
        public int LevelId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("condition")]
        public string Condition { get; set; } = string.Empty;

        [Required]
        [Column("difficulty_type_id")]
        public int DifficultyTypeId { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [Required]
        [Column("check_type_id")]
        public int CheckTypeId { get; set; }

        [ForeignKey(nameof(LevelId))]
        public Level Level { get; set; } = null!;

        [ForeignKey(nameof(DifficultyTypeId))]
        public DifficultyType DifficultyType { get; set; } = null!;

        [ForeignKey(nameof(CheckTypeId))]
        public CheckType CheckType { get; set; } = null!;

        public CustomCheckAlgorithm? CustomCheckAlgorithm { get; set; }

        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();

        public ICollection<Hint> Hints { get; set; } = new List<Hint>();

        public ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();
        public ICollection<Solution> Solutions { get; set; } = new List<Solution>();
    }
}

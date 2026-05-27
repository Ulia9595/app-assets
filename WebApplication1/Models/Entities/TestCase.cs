using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("test_cases")]
    public class TestCase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("task_id")]
        public int TaskId { get; set; }

        [Column("input_data")]
        public string? InputData { get; set; }

        [Required]
        [Column("expected_output")]
        public string ExpectedOutput { get; set; } = string.Empty;

        [Column("is_hidden")]
        public bool IsHidden { get; set; } = false;

        [ForeignKey(nameof(TaskId))]
        public PracticeTask PracticeTask { get; set; } = null!;
    }
}

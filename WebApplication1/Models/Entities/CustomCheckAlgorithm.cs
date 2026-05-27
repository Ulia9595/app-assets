using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("custom_check_algorithms")]
    public class CustomCheckAlgorithm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("task_id")]
        public int TaskId { get; set; }

        [Required]
        [Column("algorithm_code")]
        public string AlgorithmCode { get; set; } = string.Empty;

        [ForeignKey(nameof(TaskId))]
        public PracticeTask PracticeTask { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("hints")]
    public class Hint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("task_id")]
        public int TaskId { get; set; }

        [Required]
        [Column("hint_text")]
        public string HintText { get; set; } = string.Empty;

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [ForeignKey(nameof(TaskId))]
        public PracticeTask PracticeTask { get; set; } = null!;
    }
}

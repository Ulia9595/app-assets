using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models.Entities
{
    [Table("topics")]
    public class Topic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        public ICollection<Level> Levels { get; set; } = new List<Level>();

        public ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
    }
}
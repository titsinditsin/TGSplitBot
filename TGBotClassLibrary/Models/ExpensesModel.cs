using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TGBotClassLibrary
{
    [Table("expenses")]
    public class Expenses
    {
        [Key]
        [Column("e_id")]
        public int Id { get; set; }
        [Column("g_id")]
        public long GId { get; set; }
        [Column("e_cost")]
        public decimal Amount { get; set; } = 0;
        [Column("e_message")]
        public string Description { get; set; } = string.Empty;

        [ForeignKey("GId")]
        public Group group { get; set; } = null!;
    }
}

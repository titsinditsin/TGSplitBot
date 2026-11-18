using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TGBotClassLibrary
{
    [PrimaryKey(nameof(EId), nameof(UId))]
    [Table("expense_participants")]
    public class ExpenseParticipants
    {
        [Key]
        [Column("e_id")]
        public int EId { get; set; }
        [Column("u_id")]
        public long UId { get; set; }
        [Column("paid")]
        public decimal Paid { get; set; }
        [Column("owed")]
        public decimal Owed { get; set; }
        [ForeignKey("UId")]
        public User user { get; set; } = null!;
        [ForeignKey("EId")]
        public Expenses expenses { get; set; } = null!;


    }
}

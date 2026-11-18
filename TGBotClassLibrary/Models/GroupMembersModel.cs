using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace TGBotClassLibrary
{
    [PrimaryKey(nameof(GId), nameof(UId))]
    [Table("group_members")]
    public class GroupMember
    {
        [Column("g_id")]
        public long GId { get; set; }
        [Column("u_id")]
        public long UId { get; set; }
        [ForeignKey("GId")]
        public Group group { get; set; } = null!;
        [ForeignKey("UId")]
        public User user { get; set; } = null!;
    }
}

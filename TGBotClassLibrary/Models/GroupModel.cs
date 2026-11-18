using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TGBotClassLibrary
{
    [Table("groups")]
    public class Group
    {
        [Key]
        [Column("g_id")]
        public long Id { get; set; }
        [Column("g_type")]
        public string GType { get; set; } = string.Empty;
        [Column("g_name")]
        public string GName { get; set; } = string.Empty;
    }
}

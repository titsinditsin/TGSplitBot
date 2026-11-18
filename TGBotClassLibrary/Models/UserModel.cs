using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TGBotClassLibrary
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("u_id")]
        public long Id { get; set; }
        [Column("u_name")]
        public string Name { get; set; } = string.Empty;
    }
}
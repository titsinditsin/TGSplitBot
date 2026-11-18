using Microsoft.EntityFrameworkCore;
using TGBotClassLibrary;
namespace TGBotClassLibrary
{
    public class SplitDbContext : DbContext
    {
        public SplitDbContext(DbContextOptions<SplitDbContext> options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        public DbSet<ExpenseParticipants> ExpenseParticipants { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Expenses> Expenses { get; set; }
        public DbSet<Group> Groups { get; set; }
    }
}
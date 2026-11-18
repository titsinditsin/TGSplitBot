using System.Data;
using Microsoft.EntityFrameworkCore;

namespace TGBotClassLibrary.Repositories.GroupMemberRepository
{
    /// <summary>
    /// Репозиторий для связи пользователей и групп (участники чата).
    /// </summary>
    public class GroupMemberRepository
    {
        private readonly SplitDbContext _context;
        public GroupMemberRepository(SplitDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Привязка пользователя к группе.
        /// </summary>
        public async Task AddMember(long groupId, long userId)
        {
            if (!await _context.GroupMembers.AnyAsync(gm => gm.GId == groupId && gm.UId == userId))
            {
                await _context.GroupMembers.AddAsync(new GroupMember
                {
                    GId = groupId,
                    UId = userId
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveMember(long groupId, long userId)
        {
            await _context.GroupMembers.Where(gm => gm.GId == groupId && gm.UId == userId).ExecuteDeleteAsync();
        }

        public async Task<IEnumerable<(long UserId, string UserName)>> GetMembers(long groupId)
        { 
        var members = await _context.GroupMembers
        .Where(gm => gm.GId == groupId)
        .Select(gm => new
        {
            UserId = gm.UId,
            UserName = gm.user.Name
        })
        .ToListAsync();
            return members.Select(m => (m.UserId, m.UserName));
        }

        public async Task<bool> IsMember(long groupId, long userId)
        {
            return await _context.GroupMembers.AnyAsync(gm => gm.GId == groupId && gm.UId == userId); ;
        }

        /// <summary>
        /// Проверяет, есть ли в группе участник с указанным именем (без учёта регистра).
        /// </summary>
        public async Task<bool> MemberNameExistsInGroup(long groupId, string memberName)
        {
            return await _context.GroupMembers.AnyAsync(gm => gm.GId == groupId && gm.user.Name.ToLower() == memberName.ToLower());
        }
    }
}
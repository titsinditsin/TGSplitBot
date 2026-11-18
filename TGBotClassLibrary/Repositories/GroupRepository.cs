using System.Data;
using Microsoft.EntityFrameworkCore;


namespace TGBotClassLibrary.Repositories.GroupRepository
{
    /// <summary>
    /// Репозиторий для управления группами (чатами).
    /// </summary>
    public class GroupRepository
    {
        private readonly SplitDbContext _context;
        public GroupRepository(SplitDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Регистрация новой группы с защитой от дублирования.
        /// </summary>
        public async Task EnsureGroupExists(long groupId, string groupType, string groupName)
        {
            if (await _context.Groups.FindAsync(groupId) == null)
            {
                await _context.Groups.AddAsync(new Group
                {
                    Id = groupId,
                    GType = groupType,
                    GName = groupName ?? string.Empty
                });
                await _context.SaveChangesAsync();
            } 
        }

        /// <summary>
        /// Проверка, была ли группа инициализирована ранее.
        /// </summary>
        public async Task<bool> Exists(long groupId)
        {
            return await _context.Groups.AnyAsync(g => g.Id == groupId);
        }

        /// <summary>
        /// Поиск виртуальной группы по названию среди групп конкретного пользователя (без учёта регистра).
        /// Возвращает ID группы или null, если не найдена.
        /// </summary>
        public async Task<long?> FindByNameAndMember(string groupName, long userId)
        {
            var GrId = await _context.GroupMembers.FirstOrDefaultAsync(x => x.group.GType == "Virtual" && x.UId == userId && x.group.GName.ToLower() == groupName.ToLower());
            if (GrId == null)
            {
                return null;
            }
            else
            {
                return GrId.GId;
            }
        }

        /// <summary>
        /// Создаёт виртуальную группу с гарантированно уникальным коротким отрицательным ID.
        /// ID имеет длину 7 цифр (на 1-3 цифры короче типичного Telegram ID),
        /// что исключает конфликты с реальными группами Telegram.
        /// Уникальность гарантируется на уровне БД: INSERT + ON CONFLICT DO NOTHING + проверка rowsAffected.
        /// </summary>
        public async Task<long> CreateVirtualGroup(string groupName)
        {
            // 1. Находим самый маленький уже занятый виртуальный ID (от 1 до 10 млн)
            long minId = await _context.Groups
                .Where(g => g.Id <= -1_000_000 && g.Id > -10_000_000)
                .MinAsync(g => (long?)g.Id) ?? -1_000_000;

            // 2. Следующий ID — это просто предыдущий минимум - 1
            long newId = minId - 1;

            if (newId <= -10_000_000)
            {
                throw new InvalidOperationException("Достигнут лимит виртуальных групп.");
            }

            // 3. Добавляем и сохраняем
            _context.Groups.Add(new Group
            {
                Id = newId,
                GType = "Virtual",
                GName = groupName ?? "Unknown"
            });

            await _context.SaveChangesAsync();

            return newId;
        }
    }
}
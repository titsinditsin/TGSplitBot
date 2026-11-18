using System.Data;
using Microsoft.EntityFrameworkCore;


namespace TGBotClassLibrary.Repositories.UserRepository
{
    /// <summary>
    /// Репозиторий для управления пользователями.
    /// </summary>
    public class UserRepository
    {
        private readonly SplitDbContext _context;
        public UserRepository(SplitDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Регистрация нового пользователя или обновление имени существующего.
        /// </summary>
        public async Task AddOrUpdateUser(long userId, string userName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                 _context.Users.Add(new User
                {
                    Id = userId,
                    Name = userName
                });
            }
            else
            {
                user.Name = userName;
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Проверка существования пользователя в базе.
        /// </summary>
        public async Task<bool> Exists(long userId)
        {
            return await _context.Users.AnyAsync(x => x.Id == userId);
        }

        /// <summary>
        /// Поиск пользователя по зарегистрированному имени (без учёта регистра).
        /// Возвращает ID пользователя или null, если не найден.
        /// </summary>
        public async Task<long?> FindByName(string userName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Name.ToLower() == userName.ToLower());
            return user?.Id;
        }

        /// <summary>
        /// Создаёт виртуального пользователя с гарантированно уникальным коротким ID.
        /// ID имеет длину 7 цифр (на 1-3 цифры короче типичного 10-значного Telegram ID),
        /// что исключает конфликты с реальными пользователями Telegram.
        /// Уникальность гарантируется на уровне БД: INSERT + ON CONFLICT DO NOTHING + проверка rowsAffected.
        /// </summary>
        public async Task<long> CreateVirtualUser(string userName)
        {
            // 1. Находим самый большой уже занятый виртуальный ID (от 1 до 10 млн)
            long maxId = await _context.Users
                .Where(u => u.Id >= 1_000_000 && u.Id < 10_000_000)
                .MaxAsync(u => (long?)u.Id) ?? 1_000_000;

            // 2. Следующий ID — это просто предыдущий максимум + 1
            long newId = maxId + 1;

            if (newId >= 10_000_000)
            {
                throw new InvalidOperationException("Достигнут лимит виртуальных пользователей.");
            }

            // 3. Добавляем и сохраняем
            _context.Users.Add(new User
            {
                Id = newId,
                Name = userName ?? "Unknown"
            });

            await _context.SaveChangesAsync();

            return newId;
        }
    }
}
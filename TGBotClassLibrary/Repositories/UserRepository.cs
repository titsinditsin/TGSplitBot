using System.Data;
using Dapper;
using Npgsql;

namespace TGBotClassLibrary.Repositories.UserRepository
{
    /// <summary>
    /// Репозиторий для управления пользователями.
    /// </summary>
    public class UserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Регистрация нового пользователя или обновление имени существующего.
        /// </summary>
        public void AddOrUpdateUser(long userId, string userName)
        {
            const string sql = @"
                INSERT INTO users (u_id, u_name)
                VALUES (@UserId, @UserName)
                ON CONFLICT (u_id) DO UPDATE SET u_name = EXCLUDED.u_name;";

            _connection.Execute(sql, new { UserId = userId, UserName = userName ?? "Unknown" });
        }

        /// <summary>
        /// Проверка существования пользователя в базе.
        /// </summary>
        public bool Exists(long userId)
        {
            const string sql = "SELECT EXISTS (SELECT 1 FROM users WHERE u_id = @UserId);";
            return _connection.ExecuteScalar<bool>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Поиск пользователя по зарегистрированному имени (без учёта регистра).
        /// Возвращает ID пользователя или null, если не найден.
        /// </summary>
        public long? FindByName(string userName)
        {
            const string sql = "SELECT u_id FROM users WHERE LOWER(u_name) = LOWER(@UserName) LIMIT 1;";
            return _connection.QuerySingleOrDefault<long?>(sql, new { UserName = userName });
        }

        /// <summary>
        /// Создаёт виртуального пользователя с гарантированно уникальным коротким ID.
        /// ID имеет длину 7 цифр (на 1-3 цифры короче типичного 10-значного Telegram ID),
        /// что исключает конфликты с реальными пользователями Telegram.
        /// Уникальность гарантируется на уровне БД: INSERT + ON CONFLICT DO NOTHING + проверка rowsAffected.
        /// </summary>
        public long CreateVirtualUser(string userName)
        {
            var random = new Random();
            const int maxAttempts = 100;

            const string sql = @"
                INSERT INTO users (u_id, u_name)
                VALUES (@UserId, @UserName)
                ON CONFLICT (u_id) DO NOTHING;";

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                long id = random.NextInt64(1_000_000, 10_000_000);
                int rowsAffected = _connection.Execute(sql, new { UserId = id, UserName = userName ?? "Unknown" });

                if (rowsAffected > 0)
                    return id;
            }

            throw new InvalidOperationException(
                "Не удалось сгенерировать уникальный ID для виртуального пользователя.");
        }
    }
}
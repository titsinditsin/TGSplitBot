using System.Data;
using Dapper;
using Npgsql;

namespace TGBotClassLibrary.Repositories.GroupRepository
{
    /// <summary>
    /// Репозиторий для управления группами (чатами).
    /// </summary>
    public class GroupRepository
    {
        private readonly IDbConnection _connection;

        public GroupRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Регистрация новой группы с защитой от дублирования.
        /// </summary>
        public void EnsureGroupExists(long groupId, string groupType, string groupName)
        {
            const string sql = @"
                INSERT INTO groups (g_id, g_type, g_name)
                VALUES (@GroupId, @GroupType, @GroupName)
                ON CONFLICT (g_id) DO NOTHING;";

            _connection.Execute(sql, new
            {
                GroupId = groupId,
                GroupType = groupType,
                GroupName = groupName ?? string.Empty
            });
        }

        /// <summary>
        /// Проверка, была ли группа инициализирована ранее.
        /// </summary>
        public bool Exists(long groupId)
        {
            const string sql = "SELECT EXISTS (SELECT 1 FROM groups WHERE g_id = @GroupId);";
            return _connection.ExecuteScalar<bool>(sql, new { GroupId = groupId });
        }

        /// <summary>
        /// Поиск виртуальной группы по названию среди групп конкретного пользователя (без учёта регистра).
        /// Возвращает ID группы или null, если не найдена.
        /// </summary>
        public long? FindByNameAndMember(string groupName, long userId)
        {
            const string sql = @"
                SELECT g.g_id FROM groups g
                INNER JOIN group_members gm ON g.g_id = gm.g_id
                WHERE LOWER(g.g_name) = LOWER(@GroupName)
                  AND g.g_type = 'Virtual'
                  AND gm.u_id = @UserId
                LIMIT 1;";
            return _connection.QuerySingleOrDefault<long?>(sql, new { GroupName = groupName, UserId = userId });
        }

        /// <summary>
        /// Создаёт виртуальную группу с гарантированно уникальным коротким отрицательным ID.
        /// ID имеет длину 7 цифр (на 1-3 цифры короче типичного Telegram ID),
        /// что исключает конфликты с реальными группами Telegram.
        /// Уникальность гарантируется на уровне БД: INSERT + ON CONFLICT DO NOTHING + проверка rowsAffected.
        /// </summary>
        public long CreateVirtualGroup(string groupName)
        {
            var random = new Random();
            const int maxAttempts = 100;

            const string sql = @"
                INSERT INTO groups (g_id, g_type, g_name)
                VALUES (@GroupId, 'Virtual', @GroupName)
                ON CONFLICT (g_id) DO NOTHING;";

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                long id = -random.NextInt64(1_000_000, 10_000_000);
                int rowsAffected = _connection.Execute(sql, new { GroupId = id, GroupName = groupName ?? string.Empty });

                if (rowsAffected > 0)
                    return id;
            }

            throw new InvalidOperationException(
                "Не удалось сгенерировать уникальный ID для виртуальной группы.");
        }
    }
}
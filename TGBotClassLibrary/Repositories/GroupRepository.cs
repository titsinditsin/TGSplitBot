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
    }
}
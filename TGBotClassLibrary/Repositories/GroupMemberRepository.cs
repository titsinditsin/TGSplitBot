using System.Data;
using System.Collections.Generic;
using Dapper;
using Npgsql;

namespace TGBotClassLibrary.Repositories.GroupMemberRepository
{
    /// <summary>
    /// Репозиторий для связи пользователей и групп (участники чата).
    /// </summary>
    public class GroupMemberRepository
    {
        private readonly IDbConnection _connection;

        public GroupMemberRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Привязка пользователя к группе.
        /// </summary>
        public void AddMember(long groupId, long userId)
        {
            const string sql = @"
                INSERT INTO group_members (g_id, u_id)
                VALUES (@GroupId, @UserId)
                ON CONFLICT (g_id, u_id) DO NOTHING;";

            _connection.Execute(sql, new { GroupId = groupId, UserId = userId });
        }

        public void RemoveMember(long groupId, long userId)
        {
            const string sql = "DELETE FROM group_members WHERE g_id = @GroupId AND u_id = @UserId;";
            _connection.Execute(sql, new { GroupId = groupId, UserId = userId });
        }

        public IEnumerable<(long UserId, string UserName)> GetMembers(long groupId)
        {
            const string sql = @"
                SELECT u.u_id, u.u_name
                FROM group_members gm
                INNER JOIN users u ON gm.u_id = u.u_id
                WHERE gm.g_id = @GroupId;";

            return _connection.Query<(long, string)>(sql, new { GroupId = groupId });
        }

        public bool IsMember(long groupId, long userId)
        {
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1 FROM group_members
                    WHERE g_id = @GroupId AND u_id = @UserId
                );";
            return _connection.ExecuteScalar<bool>(sql, new { GroupId = groupId, UserId = userId });
        }
    }
}
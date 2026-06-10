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
    }
}
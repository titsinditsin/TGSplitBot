using System.Collections.Generic;
using Dapper;
using Npgsql;

namespace TGBotClassLibrary.Repositories.ExpensesRepository
{
    /// <summary>
    /// Репозиторий для работы с общими расходами группы.
    /// </summary>
    public class ExpensesRepository
    {
        private readonly NpgsqlConnection _connection;

        public ExpensesRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Создает новый расход и возвращает его идентификатор (e_id).
        /// </summary>
        public long AddExpense(long groupId, decimal cost, string message)
        {
            const string sql = @"
                INSERT INTO expenses (g_id, e_cost, e_message)
                VALUES (@GroupId, @Cost, @Message)
                RETURNING e_id;";

            return _connection.ExecuteScalar<long>(sql, new
            {
                GroupId = groupId,
                Cost = cost,
                Message = message ?? string.Empty
            });
        }

        public IEnumerable<(long Id, decimal Cost, string Message, long GroupId)> GetExpensesByGroup(long groupId)
        {
            const string sql = @"
                SELECT e_id, e_cost, e_message, g_id
                FROM expenses
                WHERE g_id = @GroupId
                ORDER BY e_id DESC;";

            return _connection.Query<(long, decimal, string, long)>(sql, new { GroupId = groupId });
        }
    }
}
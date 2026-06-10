using System.Collections.Generic;
using Dapper;
using Npgsql;

namespace TGBotClassLibrary.Repositories.ExpenseParticipantsRepository
{
    /// <summary>
    /// Репозиторий для фиксации долей и выплат конкретного расхода.
    /// </summary>
    public class ExpenseParticipantsRepository
    {
        private readonly NpgsqlConnection _connection;

        public ExpenseParticipantsRepository(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Добавляет участника в чек (с указанием внесенной суммы и размера долга).
        /// </summary>
        public void AddParticipant(long expenseId, long userId, decimal paid, decimal owed)
        {
            const string sql = @"
                INSERT INTO expense_participants (e_id, u_id, paid, owed)
                VALUES (@ExpenseId, @UserId, @Paid, @Owed)
                ON CONFLICT (e_id, u_id) DO NOTHING;";

            _connection.Execute(sql, new
            {
                ExpenseId = expenseId,
                UserId = userId,
                Paid = paid,
                Owed = owed
            });
        }

        public IEnumerable<(long UserId, decimal Paid, decimal Owed)> GetParticipants(long expenseId)
        {
            const string sql = "SELECT u_id, paid, owed FROM expense_participants WHERE e_id = @ExpenseId;";
            return _connection.Query<(long, decimal, decimal)>(sql, new { ExpenseId = expenseId });
        }
    }
}
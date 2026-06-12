using System.Collections.Generic;
using System.Data;
using Dapper;
using Npgsql;

namespace TGBotClassLibrary.Repositories.ExpenseParticipantsRepository
{
    /// <summary>
    /// Репозиторий для фиксации долей и выплат конкретного расхода.
    /// </summary>
    public class ExpenseParticipantsRepository
    {
        private readonly IDbConnection _connection;

        public ExpenseParticipantsRepository(IDbConnection connection)
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

        /// <summary>
        /// Возвращает общий баланс по каждому участнику группы (сумма paid - сумма owed).
        /// Положительный баланс означает, что пользователю должны. Отрицательный - он должен.
        /// </summary>
        public IEnumerable<UserBalance> GetBalancesByGroup(long groupId)
        {
            const string sql = @"
                SELECT u.u_id as UserId, u.u_name as UserName, COALESCE(SUM(ep.paid - ep.owed), 0) as Balance
                FROM users u
                INNER JOIN group_members gm ON u.u_id = gm.u_id
                LEFT JOIN expenses e ON e.g_id = gm.g_id
                LEFT JOIN expense_participants ep ON ep.e_id = e.e_id AND ep.u_id = u.u_id
                WHERE gm.g_id = @GroupId
                GROUP BY u.u_id, u.u_name;";
            
            return _connection.Query<UserBalance>(sql, new { GroupId = groupId });
        }
        public IEnumerable<UserHistoryItem> GetUserHistory(long groupId, long userId)
        {
            const string sql = @"SELECT paid, owed, e_message, e_time  FROM expense_participants JOIN expenses ON expenses.e_id =
  expense_participants.e_id WHERE g_id = @group_id AND u_id = @userId;";
            return _connection.Query<UserHistoryItem>(sql, new { group_id = groupId, userId = userId }); ;
        }
    }
}
using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Telegram.Bot.Types;

namespace TGBotClassLibrary.Repositories.ExpensesRepository
{
    /// <summary>
    /// Репозиторий для работы с общими расходами группы.
    /// </summary>
    public class ExpensesRepository
    {
        private readonly SplitDbContext _context;
        public ExpensesRepository(SplitDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Создает новый расход и возвращает его идентификатор (e_id).
        /// </summary>
        public async Task<int> AddExpense(long groupId, decimal cost, string message)
        {
            var expense = new Expenses
            {
                GId = groupId,
                Amount = cost,
                Description = message ?? string.Empty
            };
            await _context.Expenses.AddAsync(expense);
            await _context.SaveChangesAsync();
            return expense.Id;
        }

        public async Task<IEnumerable<(int Id, decimal Cost, string Message, long GroupId)>> GetExpensesByGroup(long groupId)
        {
            var expenses = await _context.Expenses
                .Where(e => e.GId == groupId)
                .OrderByDescending(e => e.Id)
                .Select(e => new
                {
                    Id = e.Id,
                    Cost = e.Amount,
                    Message = e.Description,
                    GroupId = e.GId
                }).ToListAsync();

            return expenses.Select(e => (e.Id,e.Cost,e.Message, e.GroupId));
        }
    }
}
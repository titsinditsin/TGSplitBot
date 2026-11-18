using System.Collections.Generic;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Telegram.Bot.Types;


namespace TGBotClassLibrary.Repositories.ExpenseParticipantsRepository
{
    /// <summary>
    /// Репозиторий для фиксации долей и выплат конкретного расхода.
    /// </summary>
    public class ExpenseParticipantsRepository
    {
        private readonly SplitDbContext _context;

        public ExpenseParticipantsRepository(SplitDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Добавляет участника в чек (с указанием внесенной суммы и размера долга).
        /// </summary>
        public async Task AddParticipant(int expenseId, long userId, decimal paid, decimal owed)
        {
            _context.ExpenseParticipants.Add(new ExpenseParticipants
            {
                EId = expenseId,
                UId = userId,
                Paid = paid,
                Owed = owed
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<ExpenseParticipants>> GetParticipants(int expenseId)
        {
            return await _context.ExpenseParticipants.Where(x => x.EId == expenseId).ToListAsync();
        }

        /// <summary>
        /// Возвращает общий баланс по каждому участнику группы (сумма paid - сумма owed).
        /// Положительный баланс означает, что пользователю должны. Отрицательный - он должен.
        /// </summary>
        public async Task<List<UserBalance>>  GetBalancesByGroup(long groupId)
        {

            return await _context.GroupMembers
                // 1. Фильтруем участников:
                .Where(gm => gm.GId == groupId)
                .Select(gm => new UserBalance
                {
                    UserId = gm.UId,
                    UserName = gm.user.Name,
                    Balance = _context.ExpenseParticipants.Where(ep => ep.UId == gm.UId && ep.expenses.GId == groupId).Sum(ep => (decimal?)(ep.Paid - ep.Owed)) ?? 0
                }).ToListAsync();
        }
        public async Task<List<UserHistoryItem>> GetUserHistory(long groupId, long userId)
        {
            return await _context.ExpenseParticipants
                // 1. Фильтруем участников:
                .Where(ep => ep.expenses.GId == groupId && ep.UId == userId)
                    
                // 2. Упаковываем в DTO истории:
                .Select(ep => new UserHistoryItem
                {
                    paid = ep.Paid,
                    owed = ep.Owed,
                    e_message = ep.expenses.Description // достали текст чека напрямую через навигацию!
                })

                // 3. Отправляем в базу:
                .ToListAsync();
        }
    }
}
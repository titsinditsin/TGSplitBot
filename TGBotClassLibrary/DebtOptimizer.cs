using System;
using System.Collections.Generic;
using System.Linq;

namespace TGBotClassLibrary
{
    /// <summary>
    /// Сервис для оптимизации (схлопывания) долгов между участниками группы.
    /// </summary>
    public static class DebtOptimizer
    {
        /// <summary>
        /// Принимает список балансов пользователей и возвращает список оптимальных переводов
        /// для полного погашения всех долгов внутри группы.
        /// </summary>
        public static List<DebtTransfer> Optimize(List<UserBalance> balances)
        {
            var transfers = new List<DebtTransfer>();

            // Создаем копии данных, чтобы не менять исходные объекты
            var debtors = balances
                .Where(b => b.Balance < 0)
                .Select(b => new { b.UserName, Amount = -b.Balance })
                .OrderByDescending(d => d.Amount)
                .ToList();

            var creditors = balances
                .Where(b => b.Balance > 0)
                .Select(b => new { b.UserName, Amount = b.Balance })
                .OrderByDescending(c => c.Amount)
                .ToList();

            int i = 0, j = 0;
            while (i < debtors.Count && j < creditors.Count)
            {
                var debtor = debtors[i];
                var creditor = creditors[j];

                decimal transferAmount = Math.Min(debtor.Amount, creditor.Amount);

                transfers.Add(new DebtTransfer
                {
                    FromUserName = debtor.UserName,
                    ToUserName = creditor.UserName,
                    Amount = transferAmount
                });

                // Обновляем суммы
                debtors[i] = new { debtor.UserName, Amount = debtor.Amount - transferAmount };
                creditors[j] = new { creditor.UserName, Amount = creditor.Amount - transferAmount };

                if (debtors[i].Amount <= 0) i++;
                if (creditors[j].Amount <= 0) j++;
            }

            return transfers;
        }
    }
}

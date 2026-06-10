using System.Collections.Generic;
using Telegram.Bot.Types;

namespace TGBotClassLibrary
{
    /// <summary>
    /// Представляет пользователя Telegram.
    /// </summary>
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Представляет чат (группу или супергруппу) в Telegram.
    /// </summary>
    public class Group
    {
        public long Id { get; set; }
        public string GType { get; set; } = string.Empty;
        public string GName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Модель связи (М:М), указывающая, что пользователь состоит в определенной группе.
    /// </summary>
    public class GroupMember
    {
        public long GId { get; set; }
        public long UId { get; set; }
    }

    /// <summary>
    /// Сущность расхода в конкретной группе.
    /// </summary>
    public class Expenses
    {
        public long Id { get; set; }
        public long GId { get; set; }
        public decimal Amount { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Детализация долей: кто сколько заплатил и кто сколько должен по конкретному расходу.
    /// </summary>
    public class ExpenseParticipants
    {
        public long EId { get; set; }
        public long UId { get; set; }
        public decimal Paid { get; set; }
        public decimal Owed { get; set; }
    }

    /// <summary>
    /// Результат парсинга сообщения.
    /// </summary>
    public class ParsedCommand
    {
        public string CommandType { get; set; } = "unknown";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public static class MessageParser
    {
        /// <summary>
        /// Разбирает входящий текст на команды.
        /// </summary>
        public static ParsedCommand Parse(string text, MessageEntity[]? entities)
        {
            if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("/"))
            {
                return new ParsedCommand { CommandType = "unknown" };
            }

            string[] parts = text.Split(' ', 2);
            string cmd = parts[0].ToLower();

            return cmd switch
            {
                "/start" => new ParsedCommand { CommandType = "start" },
                "/init" => new ParsedCommand { CommandType = "init" },
                "/join" => new ParsedCommand { CommandType = "join" },
                _ => new ParsedCommand { CommandType = "unknown" }
            };
        }
    }
}
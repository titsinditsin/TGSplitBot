using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace TGBotClassLibrary
{
    /// <summary>
    /// Результат парсинга сообщения.
    /// </summary>

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

            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLower();

            return cmd switch
            {
                "/start" => new ParsedCommand { CommandType = "start" },
                "/init" => new ParsedCommand { CommandType = "init" },
                "/join" => ParseJoinCommand(parts),
                "/info" => new ParsedCommand { CommandType = "info" },
                "/group" => ParseGroupCommand(parts),
                "/paid" => ParsePaidCommand(parts),
                "/payfor" => ParsePayforCommand(parts),
                "/return" => ParseReturnCommand(parts),
                "/balance" => ParseBalanceCommand(parts),
                "/history" => ParseHistoryCommand(parts),
                _ => new ParsedCommand { CommandType = "unknown" }
            };
        }

        private static ParsedCommand ParsePayforCommand(string[] parts)
        {
            var result = new ParsedCommand { CommandType = "payfor" };
            result.Parameters["args"] = parts.Skip(1).ToArray();
            return result;
        }

        private static ParsedCommand ParseReturnCommand(string[] parts)
        {
            var result = new ParsedCommand { CommandType = "return" };
            result.Parameters["args"] = parts.Skip(1).ToArray();
            return result;
        }

        private static ParsedCommand ParseBalanceCommand(string[] parts)
        {
            var result = new ParsedCommand { CommandType = "balance" };
            result.Parameters["args"] = parts.Skip(1).ToArray();
            return result;
        }

        /// <summary>
        /// Разбирает команду /paid.
        /// Возвращает все аргументы команды для дальнейшей обработки в зависимости от типа чата.
        /// </summary>
        private static ParsedCommand ParsePaidCommand(string[] parts)
        {
            var result = new ParsedCommand { CommandType = "paid" };
            result.Parameters["args"] = parts.Skip(1).ToArray();
            return result;
        }

        /// <summary>
        /// Разбирает команду /join.
        /// В групповом чате: /join (без параметров)
        /// В ЛС: /join НазваниеГруппы ИмяУчастника
        /// </summary>
        private static ParsedCommand ParseJoinCommand(string[] parts)
        {
            var result = new ParsedCommand { CommandType = "join" };

            if (parts.Length >= 3)
            {
                result.Parameters["group_name"] = parts[1];
                result.Parameters["member_name"] = parts[2];
            }

            return result;
        }

        /// <summary>
        /// Разбирает команду /group.
        /// Формат: /group НазваниеГруппы Участник1 Участник2 ...
        /// </summary>
        private static ParsedCommand ParseGroupCommand(string[] parts)
        {
            var result = new ParsedCommand { CommandType = "group" };

            if (parts.Length < 2)
            {
                result.Parameters["error"] = "no_group_name";
                return result;
            }

            result.Parameters["group_name"] = parts[1];

            var members = new List<string>();
            for (int i = 2; i < parts.Length; i++)
            {
                members.Add(parts[i]);
            }
            result.Parameters["members"] = members;

            return result;
        }


        private static ParsedCommand ParseHistoryCommand(string[] parts)
        {
            var result = new ParsedCommand { CommandType = "history" };
            result.Parameters["args"] = parts.Skip(1).ToArray();
            return result;
        }

    }

}
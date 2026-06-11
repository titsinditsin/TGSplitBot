using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TGBotClassLibrary;
using TGBotClassLibrary.Repositories.GroupMemberRepository;
using TGBotClassLibrary.Repositories.GroupRepository;
using TGBotClassLibrary.Repositories.UserRepository;
using TGBotClassLibrary.Repositories.ExpensesRepository;
using TGBotClassLibrary.Repositories.ExpenseParticipantsRepository;
using System.Linq;

string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Ошибка: Переменная окружения DB_CONNECTION не найдена!");
    return;
}

string botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
if (string.IsNullOrEmpty(botToken))
{
    Console.WriteLine("Ошибка: Переменная окружения BOT_TOKEN не найдена!");
    return;
}

var botClient = new TelegramBotClient(botToken);

/// <summary>
/// Основной обработчик входящих обновлений от Telegram.
/// </summary>
async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
{
    // Обрабатываем только текстовые сообщения
    if (update.Message is not { Text: { } messageText }) return;

    // Извлекаем данные пользователя
    var user = new TGBotClassLibrary.User
    {
        Id = update.Message.From.Id,
        Name = update.Message.From.FirstName
    };

    // Извлекаем данные группы/чата
    var group = new TGBotClassLibrary.Group
    {
        Id = update.Message.Chat.Id,
        GType = update.Message.Chat.Type.ToString(),
        GName = update.Message.Chat.Title ?? string.Empty
    };

    // Парсим текст сообщения на предмет команд
    var parsedCommand = MessageParser.Parse(messageText, update.Message.Entities);

    // Открываем одно соединение с БД для обработки текущего запроса
    using var connection = new NpgsqlConnection(connectionString);
    var userRepo = new UserRepository(connection);
    var groupRepo = new GroupRepository(connection);
    var memberRepo = new GroupMemberRepository(connection);
    var expensesRepo = new ExpensesRepository(connection);
    var participantsRepo = new ExpenseParticipantsRepository(connection);

    switch (parsedCommand.CommandType)
    {
        case "start":
            userRepo.AddOrUpdateUser(user.Id, user.Name);
            await client.SendMessage(
                group.Id,
                $"👋 Привет, {user.Name}!\n\n" +
                "Я — SplitBot, твой помощник для учёта совместных расходов.\n" +
                "Создавай группы, добавляй участников и веди учёт трат — " +
                "я подскажу, кто кому сколько должен.\n\n" +
                "📖 Напиши /info чтобы узнать, как пользоваться ботом.",
                cancellationToken: cancellationToken);
            break;

        case "init":
            // Инициализация работает только в групповых чатах
            if (group.GType == ChatType.Group.ToString() || group.GType == ChatType.Supergroup.ToString())
            {
                groupRepo.EnsureGroupExists(group.Id, group.GType, group.GName);
                memberRepo.AddMember(group.Id, user.Id);

                await client.SendMessage(
                    group.Id,
                    "Бот успешно активирован для этого чата!",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await client.SendMessage(
                    group.Id,
                    "Команду /init можно использовать только внутри группового чата.",
                    cancellationToken: cancellationToken);
            }
            break;

        case "join":
            if (group.GType == ChatType.Group.ToString() || group.GType == ChatType.Supergroup.ToString())
            {
                // Режим группового чата: /join (без параметров)
                if (parsedCommand.Parameters.ContainsKey("group_name"))
                {
                    await client.SendMessage(
                        group.Id,
                        "Команда /join с параметрами работает только в личных сообщениях с ботом.\n" +
                        "В групповом чате просто напишите /join чтобы присоединиться.",
                        cancellationToken: cancellationToken);
                    break;
                }

                if (!groupRepo.Exists(group.Id))
                {
                    await client.SendMessage(
                        group.Id,
                        "Бот не инициализирован в этой группе. Сначала выполните команду /init.",
                        cancellationToken: cancellationToken);
                    break;
                }

                userRepo.AddOrUpdateUser(user.Id, user.Name);

                if (memberRepo.IsMember(group.Id, user.Id))
                {
                    await client.SendMessage(
                        group.Id,
                        "Вы уже являетесь участником этой группы.",
                        cancellationToken: cancellationToken);
                }
                else
                {
                    memberRepo.AddMember(group.Id, user.Id);
                    await client.SendMessage(
                        group.Id,
                        $"{user.Name} успешно добавлен в список участников.",
                        cancellationToken: cancellationToken);
                }
            }
            else if (group.GType == ChatType.Private.ToString())
            {
                // Режим ЛС: /join НазваниеГруппы ИмяУчастника
                if (!parsedCommand.Parameters.ContainsKey("group_name") ||
                    !parsedCommand.Parameters.ContainsKey("member_name"))
                {
                    await client.SendMessage(
                        group.Id,
                        "В личных сообщениях /join используется для добавления участника в группу.\n" +
                        "Формат: /join НазваниеГруппы ИмяУчастника\n" +
                        "Пример: /join Поездка Sergey\n\n" +
                        "Чтобы присоединиться самому — напишите /join в групповом чате.",
                        cancellationToken: cancellationToken);
                    break;
                }

                string joinGroupName = (string)parsedCommand.Parameters["group_name"];
                string joinMemberName = (string)parsedCommand.Parameters["member_name"];

                // Ищем виртуальную группу по названию среди групп этого пользователя
                long? foundGroupId = groupRepo.FindByNameAndMember(joinGroupName, user.Id);
                if (!foundGroupId.HasValue)
                {
                    await client.SendMessage(
                        group.Id,
                        $"Группа \"{joinGroupName}\" не найдена среди ваших групп. Сначала создайте её командой /group.",
                        cancellationToken: cancellationToken);
                    break;
                }

                // Проверяем, нет ли уже участника с таким именем в группе
                if (memberRepo.MemberNameExistsInGroup(foundGroupId.Value, joinMemberName))
                {
                    await client.SendMessage(
                        group.Id,
                        $"Участник \"{joinMemberName}\" уже есть в группе \"{joinGroupName}\".",
                        cancellationToken: cancellationToken);
                    break;
                }

                // Создаём виртуального участника и добавляем в группу
                long newMemberId = userRepo.CreateVirtualUser(joinMemberName);
                memberRepo.AddMember(foundGroupId.Value, newMemberId);

                await client.SendMessage(
                    group.Id,
                    $"✅ Участник \"{joinMemberName}\" добавлен в группу \"{joinGroupName}\".",
                    cancellationToken: cancellationToken);
            }
            break;

        case "info":
            await client.SendMessage(
                group.Id,
                "📖 *Инструкция по SplitBot*\n" +
                "━━━━━━━━━━━━━━━━━━━━\n\n" +
                "Есть два способа вести расходы:\n\n" +
                "*🅰 Способ 1 — Групповой чат*\n\n" +
                "① Добавь бота в групповой чат\n" +
                "② Один участник пишет /init — бот активируется\n" +
                "③ Каждый участник пишет /join — так бот узнает, кто участвует\n" +
                "④ Готово! Теперь можно записывать расходы\n\n" +
                "*🅱 Способ 2 — Через ЛС (виртуальная группа)*\n\n" +
                "① Напиши боту /start в ЛС\n" +
                "② Создай группу командой:\n" +
                "   `/group НазваниеГруппы Имя1 Имя2 ...`\n" +
                "   Пример: `/group Поездка Ivan Anna`\n" +
                "③ Добавляй новых участников:\n" +
                "   `/join НазваниеГруппы ИмяУчастника`\n" +
                "   Пример: `/join Поездка Sergey`\n\n" +
                "*📋 Список команд*\n" +
                "━━━━━━━━━━━━━━━━━━━━\n" +
                "/start — регистрация в боте\n" +
                "/info — эта инструкция\n" +
                "/init — активировать бота в групповом чате\n" +
                "/join — присоединиться (в группе) или добавить участника (в ЛС)\n" +
                "/group — создать виртуальную группу в ЛС\n\n" +
                "*⚠️ Важно:*\n" +
                "• /init работает только в групповых чатах\n" +
                "• /group и /join с параметрами работают только в ЛС\n" +
                "• Участники создаются автоматически по именам\n" +
                "• Нельзя создать две группы с одинаковым названием\n" +
                "• Имена участников в группе должны быть уникальными",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: cancellationToken);
            break;

        case "group":
            // Создание виртуальной группы работает только в личных сообщениях
            if (group.GType != ChatType.Private.ToString())
            {
                await client.SendMessage(
                    group.Id,
                    "Команда /group доступна только в личных сообщениях с ботом.",
                    cancellationToken: cancellationToken);
                break;
            }

            // Проверка наличия названия группы
            if (parsedCommand.Parameters.ContainsKey("error"))
            {
                await client.SendMessage(
                    group.Id,
                    "Формат: /group НазваниеГруппы Участник1 Участник2 ...\nПример: /group Поездка Ivan Anna",
                    cancellationToken: cancellationToken);
                break;
            }

            string groupName = (string)parsedCommand.Parameters["group_name"];
            var memberNames = (List<string>)parsedCommand.Parameters["members"];

            if (memberNames.Count == 0)
            {
                await client.SendMessage(
                    group.Id,
                    "Укажите хотя бы одного участника.\nФормат: /group НазваниеГруппы Участник1 Участник2 ...",
                    cancellationToken: cancellationToken);
                break;
            }

            // Регистрируем/обновляем текущего пользователя
            userRepo.AddOrUpdateUser(user.Id, user.Name);

            // Проверяем, нет ли у пользователя группы с таким названием
            if (groupRepo.FindByNameAndMember(groupName, user.Id).HasValue)
            {
                await client.SendMessage(
                    group.Id,
                    $"У вас уже есть группа \"{groupName}\". Выберите другое название.",
                    cancellationToken: cancellationToken);
                break;
            }

            // Убираем дубликаты имён (без учёта регистра)
            var uniqueNames = memberNames
                .GroupBy(n => n.ToLower())
                .Select(g => g.First())
                .ToList();

            var duplicateNames = memberNames
                .GroupBy(n => n.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.First())
                .ToList();

            // Создаём виртуальную группу с гарантированно уникальным ID
            long virtualGroupId = groupRepo.CreateVirtualGroup(groupName);

            // Добавляем создателя группы как участника
            memberRepo.AddMember(virtualGroupId, user.Id);

            // Создаём виртуальных пользователей и добавляем их в группу
            var createdMembers = new List<(string Name, long Id)>();

            foreach (var memberName in uniqueNames)
            {
                long virtualUserId = userRepo.CreateVirtualUser(memberName);
                memberRepo.AddMember(virtualGroupId, virtualUserId);
                createdMembers.Add((memberName, virtualUserId));
            }

            // Формируем ответ
            var response = $"✅ Группа \"{groupName}\" создана!\n\n";
            response += $"👤 Вы ({user.Name}) добавлены как создатель.\n";
            response += $"👥 Созданы участники:\n";
            foreach (var (name, id) in createdMembers)
            {
                response += $"  • {name} (ID: {id})\n";
            }

            if (duplicateNames.Count > 0)
            {
                response += $"\n⚠️ Убраны дубликаты: {string.Join(", ", duplicateNames)}\n";
            }

            await client.SendMessage(
                group.Id,
                response,
                cancellationToken: cancellationToken);
            break;

        case "paid":
            var args = (string[])parsedCommand.Parameters["args"];
            userRepo.AddOrUpdateUser(user.Id, user.Name);

            if (group.GType == ChatType.Group.ToString() || group.GType == ChatType.Supergroup.ToString())
            {
                // Режим группового чата: /paid Сумма Описание
                if (!groupRepo.Exists(group.Id))
                {
                    await client.SendMessage(group.Id, "Бот не инициализирован в этой группе. Сначала выполните команду /init.", cancellationToken: cancellationToken);
                    break;
                }

                if (args.Length < 1 || !decimal.TryParse(args[0], out decimal amount) || amount <= 0)
                {
                    await client.SendMessage(group.Id, "Формат: /paid Сумма Описание\nПример: /paid 500 Пицца", cancellationToken: cancellationToken);
                    break;
                }

                string description = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "Без описания";

                var members = memberRepo.GetMembers(group.Id).ToList();
                if (members.Count == 0)
                {
                    await client.SendMessage(group.Id, "В группе нет участников. Сначала кто-то должен написать /join.", cancellationToken: cancellationToken);
                    break;
                }

                long expenseId = expensesRepo.AddExpense(group.Id, amount, description);
                decimal splitAmount = amount / members.Count;

                foreach (var member in members)
                {
                    decimal paid = member.UserId == user.Id ? amount : 0;
                    decimal owed = splitAmount;
                    participantsRepo.AddParticipant(expenseId, member.UserId, paid, owed);
                }

                await client.SendMessage(
                    group.Id,
                    $"💰 Расход добавлен: {amount} руб. ({description})\n" +
                    $"Оплатил(а): {user.Name}\n" +
                    $"Поделено на {members.Count} участников (по {splitAmount:F2} руб.).",
                    cancellationToken: cancellationToken);
            }
            else if (group.GType == ChatType.Private.ToString())
            {
                // Режим ЛС: /paid НазваниеГруппы КтоЗаплатил Сумма Описание
                if (args.Length < 3)
                {
                    await client.SendMessage(
                        group.Id,
                        "В личных сообщениях формат: /paid НазваниеГруппы КтоЗаплатил Сумма Описание\nПример: /paid Поездка Ivan 500 Бензин",
                        cancellationToken: cancellationToken);
                    break;
                }

                string pGroupName = args[0];
                string payerName = args[1];

                if (!decimal.TryParse(args[2], out decimal pAmount) || pAmount <= 0)
                {
                    await client.SendMessage(group.Id, "Сумма должна быть положительным числом.", cancellationToken: cancellationToken);
                    break;
                }

                string pDesc = args.Length > 3 ? string.Join(" ", args.Skip(3)) : "Без описания";

                long? foundGroupId = groupRepo.FindByNameAndMember(pGroupName, user.Id);
                if (!foundGroupId.HasValue)
                {
                    await client.SendMessage(group.Id, $"Группа \"{pGroupName}\" не найдена среди ваших групп.", cancellationToken: cancellationToken);
                    break;
                }

                long? payerId = userRepo.FindByName(payerName);
                if (!payerId.HasValue || !memberRepo.IsMember(foundGroupId.Value, payerId.Value))
                {
                    await client.SendMessage(group.Id, $"Участник \"{payerName}\" не найден в группе \"{pGroupName}\".", cancellationToken: cancellationToken);
                    break;
                }

                var pMembers = memberRepo.GetMembers(foundGroupId.Value).ToList();
                long pExpenseId = expensesRepo.AddExpense(foundGroupId.Value, pAmount, pDesc);
                decimal pSplitAmount = pAmount / pMembers.Count;

                foreach (var member in pMembers)
                {
                    decimal paid = member.UserId == payerId.Value ? pAmount : 0;
                    decimal owed = pSplitAmount;
                    participantsRepo.AddParticipant(pExpenseId, member.UserId, paid, owed);
                }

                await client.SendMessage(
                    group.Id,
                    $"💰 Расход добавлен в виртуальную группу \"{pGroupName}\": {pAmount} руб. ({pDesc})\n" +
                    $"Оплатил(а): {payerName}\n" +
                    $"Поделено на {pMembers.Count} участников (по {pSplitAmount:F2} руб.).",
                    cancellationToken: cancellationToken);
            }
            break;

        default:
            // Игнорируем неизвестные команды и обычный текст
            break;
    }
}

/// <summary>
/// Обработчик ошибок Telegram API.
/// </summary>
async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"Ошибка Telegram API: {exception.Message}");
    await Task.CompletedTask;
}

// Запуск бота
botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
Console.WriteLine("Бот запущен. Нажмите Enter для выхода.");
Console.ReadLine();
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TGBotClassLibrary;
using TGBotClassLibrary.Repositories.GroupMemberRepository;
using TGBotClassLibrary.Repositories.GroupRepository;
using TGBotClassLibrary.Repositories.UserRepository;

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

    switch (parsedCommand.CommandType)
    {
        case "start":
            userRepo.AddOrUpdateUser(user.Id, user.Name);
            await client.SendMessage(
                group.Id,
                $"Привет, {user.Name}! Я бот для совместных расходов.",
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
            // Присоединение к расходам также доступно только в группах
            if (group.GType == ChatType.Group.ToString() || group.GType == ChatType.Supergroup.ToString())
            {
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
            else
            {
                await client.SendMessage(
                    group.Id,
                    "Команда /join доступна только в групповых чатах.",
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
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types;
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

// Обработчик входящих сообщений
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Только текстовые сообщения
    if (update.Message is not { Text: { } messageText }) return;

    // Берём данные с апдейта
    var user = new TGBotClassLibrary.User
    {
        Id = update.Message.From.Id,
        Name = update.Message.From.FirstName
    };

    var group = new TGBotClassLibrary.Group
    {
        Id = update.Message.Chat.Id,
        GType = update.Message.Chat.Type.ToString(),
        GName = update.Message.Chat.Title ?? string.Empty
    };

    // Парсим команду
    var parsedCommand = MessageParser.Parse(messageText, update.Message.Entities);

    // Одно соединение на все операции
    using var connection = new NpgsqlConnection(connectionString);

    // Все репозитории, которые нам нужны
    var userRepo = new UserRepository(connection);
    var groupRepo = new GroupRepository(connection);
    var memberRepo = new GroupMemberRepository(connection);
    // Остальные добавим, когда дойдём до расходов

    switch (parsedCommand.CommandType)
    {
        case "start":
            // Регистрация юзера (или обновление имени)
            userRepo.AddOrUpdateUser(user.Id, user.Name);

            await botClient.SendMessage(group.Id,
                $"Привет, {user.Name}! Я бот для совместных расходов.",
                cancellationToken: cancellationToken);
            break;

        case "init":
            // Активация бота, работает только в группах
            if (group.GType == Telegram.Bot.Types.Enums.ChatType.Group.ToString() ||
                group.GType == Telegram.Bot.Types.Enums.ChatType.Supergroup.ToString())
            {
                // Добавляем чат как группу
                groupRepo.EnsureGroupExists(group.Id, group.GType, group.GName);
                // Кидаем инициатора в участники
                memberRepo.AddMember(group.Id, user.Id);

                await botClient.SendMessage(group.Id,
                    "Бот Splitwise успешно активирован для этого чата!",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(group.Id,
                    "Команду /init можно использовать только внутри группового чата.",
                    cancellationToken: cancellationToken);
            }
            break;

        // Здесь появятся join, group, paid

        default:
            // Всё, что не распарсили — игнорируем
            break;
    }
}

// Обработчик ошибок
async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"Ошибка Telegram API: {exception.Message}");
}

// Запуск
botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
Console.WriteLine("Бот запущен. Нажмите Enter для выхода.");
Console.ReadLine();
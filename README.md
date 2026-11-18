# SplitTGBot

Telegram-бот для учёта совместных расходов внутри групп и личных чатов.  
Позволяет записывать траты, автоматически распределять долги между участниками и отслеживать баланс.


## Стек

| Технология | Назначение |
|---|---|
| C# (.NET 10) | Язык и рантайм |
| PostgreSQL | Хранение данных |
| Dapper | Работа с базой (микро-ORM) |
| Telegram.Bot | Взаимодействие с Telegram API |
| Переменные окружения | Защита секретов (.env) |


## Возможности

| Команда | Статус | Описание |
|---|---|---|
| `/start` | Готово | Регистрация пользователя в боте |
| `/init` | Готово | Активация бота в групповом чате (только для групп) |
| `/join` | Готово | Присоединение к активной группе |
| `/paid` | Готово | Запись расхода с плательщиком и описанием |
| `/payfor` | Готово | Оплата конкретно за другого человека |
| `/return` | Готово | Возврат долга конкретному человеку |
| `/balance` | Готово | Сводка баланса и алгоритм схлопывания долгов |
| `/group` | Готово | Создание виртуальной группы в личных сообщениях |
| `/history` | Готово | История покупок и переводов пользователя |


## Запуск

### 1. База данных
Установите PostgreSQL и создайте базу (например, `splitbotdb`).
Затем выполните скрипт для создания таблиц из `SplitTGBot/schema.sql`.

### 2. Переменные окружения
Задайте секреты через переменные окружения (Windows) или файл `.env`:

```cmd
setx DB_CONNECTION "Host=localhost;Port=5432;Database=splitbotdb;Username=postgres;Password=ваш_пароль"
setx BOT_TOKEN "токен_от_BotFather"
Важно: после выполнения команд перезапустите среду разработки.
```
### 3. Запуск приложения
```Bash
dotnet run --project SplitTGBot
```

### Структура проекта
```
SplitTGBot.slnx
├── SplitTGBot/                     # Консольное приложение (основной проект)
│   ├── Program.cs                  # Точка входа и обработка сообщений
│   ├── schema.sql                  # SQL-схема базы данных
│   └── SplitTGBot.csproj
├── TGBotClassLibrary/              # Библиотека классов (бизнес-логика и работа с БД)
│   ├── DTOs/                       # Объекты передачи данных
│   │   ├── DebtTransferDTO.cs
│   │   ├── ParsedCommandDTO.cs
│   │   ├── UserBalanceDTO.cs
│   │   └── UserHistoryItemDTO.cs
│   ├── Models/                     # Модели сущностей БД
│   │   ├── ExpenseParticipantsModel.cs
│   │   ├── ExpensesModel.cs
│   │   ├── GroupMembersModel.cs
│   │   ├── GroupModel.cs
│   │   └── UserModel.cs
│   ├── Parsers/                    # Парсинг входящих команд и сообщений
│   │   └── MessageParser.cs
│   ├── Repositories/               # Репозитории для работы с PostgreSQL
│   │   ├── ExpenseParticipantsRepository.cs
│   │   ├── ExpensesRepository.cs
│   │   ├── GroupMemberRepository.cs
│   │   ├── GroupRepository.cs
│   │   └── UserRepository.cs
│   ├── DBContext.cs                # Контекст подключения к базе данных
│   ├── DebtOptimizer.cs            # Алгоритм оптимизации (схлопывания) долгов
│   └── TGBotClassLibrary.csproj
└── README.md
```

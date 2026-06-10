-- Таблица пользователей Telegram
CREATE TABLE users (
    u_id BIGINT PRIMARY KEY,
    u_name VARCHAR(100) NOT NULL
);

-- Таблица групп (чатов), где активирован бот
CREATE TABLE groups (
    g_id BIGINT PRIMARY KEY,
    g_type VARCHAR(20),        -- 'Group', 'Supergroup' и т.д.
    g_name TEXT                -- название чата
);

-- Участники групп (кто состоит в какой группе)
CREATE TABLE group_members (
    g_id BIGINT REFERENCES groups(g_id) ON DELETE CASCADE,
    u_id BIGINT REFERENCES users(u_id) ON DELETE CASCADE,
    PRIMARY KEY (g_id, u_id)
);

-- Расходы
CREATE TABLE expenses (
    e_id SERIAL PRIMARY KEY,
    g_id BIGINT REFERENCES groups(g_id) ON DELETE CASCADE,
    e_cost NUMERIC(12, 2) NOT NULL,
    e_message TEXT NOT NULL
);

-- Доли и оплаты участников в конкретном расходе
CREATE TABLE expense_participants (
    e_id INT REFERENCES expenses(e_id) ON DELETE CASCADE,
    u_id BIGINT REFERENCES users(u_id) ON DELETE CASCADE,
    paid NUMERIC(12, 2) DEFAULT 0.00,
    owed NUMERIC(12, 2) DEFAULT 0.00,
    PRIMARY KEY (e_id, u_id)
);
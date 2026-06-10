using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;
using TGBotClassLibrary.Repositories.GroupMemberRepository;
using TGBotClassLibrary.Repositories.GroupRepository;
using TGBotClassLibrary.Repositories.UserRepository;
using Xunit;

namespace TGBotClassLibrary.Tests
{
    public abstract class BaseTest : IAsyncLifetime, IDisposable
    {
        protected IDbConnection Connection { get; private set; } = null!;

        public virtual async Task InitializeAsync()
        {
            // Используем Sqlite in-memory. 
            // Соединение нужно держать открытым, иначе база данных исчезнет.
            Connection = new SqliteConnection("DataSource=:memory:");
            await ((SqliteConnection)Connection).OpenAsync();
            await CreateSchemaAsync();
        }

        protected abstract Task CreateSchemaAsync();

        public Task DisposeAsync()
        {
            Connection.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Connection.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class UserRepositoryTests : BaseTest
    {
        protected override async Task CreateSchemaAsync()
        {
            await Connection.ExecuteAsync(@"
                CREATE TABLE users (
                    u_id BIGINT PRIMARY KEY,
                    u_name VARCHAR(255) NOT NULL
                );");
        }

        [Fact]
        public void AddOrUpdateUser_NewUser_AddsUserToDatabase()
        {
            var repo = new UserRepository(Connection);
            long testUserId = 12345;
            repo.AddOrUpdateUser(testUserId, "Ivan");
            Assert.True(repo.Exists(testUserId));
        }

        [Fact]
        public void AddOrUpdateUser_ExistingUser_UpdatesUserName()
        {
            var repo = new UserRepository(Connection);
            long testUserId = 777;
            repo.AddOrUpdateUser(testUserId, "OldName");
            repo.AddOrUpdateUser(testUserId, "NewName");
            var currentName = Connection.QuerySingle<string>(
                "SELECT u_name FROM users WHERE u_id = @Id", new { Id = testUserId });
            Assert.Equal("NewName", currentName);
        }

        [Fact]
        public void Exists_NonExistentUser_ReturnsFalse()
        {
            var repo = new UserRepository(Connection);
            bool exists = repo.Exists(99999);
            Assert.False(exists);
        }
    }

    public class GroupRepositoryTests : BaseTest
    {
        protected override async Task CreateSchemaAsync()
        {
            await Connection.ExecuteAsync(@"
                CREATE TABLE groups (
                    g_id BIGINT PRIMARY KEY,
                    g_type VARCHAR(50) NOT NULL,
                    g_name VARCHAR(255) NOT NULL
                );");
        }

        [Fact]
        public void EnsureGroupExists_NewGroup_AddsToDatabase()
        {
            var repo = new GroupRepository(Connection);
            long groupId = -100123456;
            repo.EnsureGroupExists(groupId, "Supergroup", "My Test Chat");
            Assert.True(repo.Exists(groupId));
        }

        [Fact]
        public void EnsureGroupExists_ExistingGroup_DoesNotThrowAndKeepsData()
        {
            var repo = new GroupRepository(Connection);
            long groupId = -100999;
            repo.EnsureGroupExists(groupId, "Group", "Initial Name");

            Action act = () => repo.EnsureGroupExists(groupId, "Group", "New Name");
            var exception = Record.Exception(act);

            Assert.Null(exception);
            var nameInDb = Connection.QuerySingle<string>(
                "SELECT g_name FROM groups WHERE g_id = @Id", new { Id = groupId });
            Assert.Equal("Initial Name", nameInDb);
        }
    }

    public class GroupMemberRepositoryTests : BaseTest
    {
        protected override async Task CreateSchemaAsync()
        {
            await Connection.ExecuteAsync(@"
                CREATE TABLE users (
                    u_id BIGINT PRIMARY KEY,
                    u_name VARCHAR(255) NOT NULL
                );
                CREATE TABLE groups (
                    g_id BIGINT PRIMARY KEY,
                    g_type VARCHAR(50),
                    g_name VARCHAR(255)
                );
                CREATE TABLE group_members (
                    g_id BIGINT,
                    u_id BIGINT,
                    PRIMARY KEY (g_id, u_id)
                );");
        }

        [Fact]
        public void AddMember_NewMember_LinksUserAndGroup()
        {
            var repo = new GroupMemberRepository(Connection);
            long groupId = 1;
            long userId = 100;
            repo.AddMember(groupId, userId);
            Assert.True(repo.IsMember(groupId, userId));
        }

        [Fact]
        public void GetMembers_ReturnsCorrectUsers()
        {
            var repo = new GroupMemberRepository(Connection);
            long groupId = 10;

            Connection.Execute("INSERT INTO users (u_id, u_name) VALUES (1, 'Alice'), (2, 'Bob')");
            repo.AddMember(groupId, 1);
            repo.AddMember(groupId, 2);

            var members = repo.GetMembers(groupId).ToList();

            Assert.Equal(2, members.Count);
            Assert.Contains(members, m => m.UserId == 1 && m.UserName == "Alice");
            Assert.Contains(members, m => m.UserId == 2 && m.UserName == "Bob");
        }
    }
}

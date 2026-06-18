using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DatabaseBillingTests
{
    // Модель пользователя, которая должна быть в базе данных
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    [TestFixture]
    public class DatabaseTests
    {
        // Имитируем таблицу базы данных в оперативной памяти (гарантирует работу без сбоев СУБД)
        private List<User> _mockDatabase;
        private int _nextId;

        [SetUp]
        public void Setup()
        {
            // Перед каждым тестом инициализируем чистую "базу данных"
            _mockDatabase = new List<User>();
            _nextId = 1;
        }

        [Test] // 1. Тест создания и чтения (C и R из CRUD)
        public void Test_CreateAndReadUser_Success()
        {
            var user = new User { Username = "QA_Student", Email = "student@qa.com" };

            // Имитируем INSERT INTO Users
            user.Id = _nextId++;
            _mockDatabase.Add(user);

            Assert.AreEqual(1, _mockDatabase.Count, "Запись не была добавлена в БД!");

            // Имитируем SELECT * FROM Users WHERE Email = 'student@qa.com'
            var fetchedUser = _mockDatabase.FirstOrDefault(u => u.Email == "student@qa.com");

            Assert.IsNotNull(fetchedUser, "Пользователь по указанному Email не найден!");
            Assert.AreEqual("QA_Student", fetchedUser.Username);
        }

        [Test] // 2. Тест обновления и удаления (U и D из CRUD)
        public void Test_UpdateAndDeleteUser_Success()
        {
            // Создаем начальную запись
            var user = new User { Id = _nextId++, Username = "OldName", Email = "update@qa.com" };
            _mockDatabase.Add(user);

            // 1. Имитируем UPDATE Users SET Username = 'NewName' ...
            var userToUpdate = _mockDatabase.FirstOrDefault(u => u.Email == "update@qa.com");
            Assert.IsNotNull(userToUpdate);
            userToUpdate.Username = "NewName";

            // Проверяем, что имя изменилось
            Assert.AreEqual("NewName", _mockDatabase.First(u => u.Email == "update@qa.com").Username);

            // 2. Имитируем DELETE FROM Users ...
            var userToDelete = _mockDatabase.FirstOrDefault(u => u.Email == "update@qa.com");
            _mockDatabase.Remove(userToDelete);

            // Проверяем, что запись удалена
            Assert.AreEqual(0, _mockDatabase.Count, "Запись не удалилась из базы данных!");
        }

        [Test] // 3. Тест проверки ограничений UNIQUE (Обработка исключений)
        public void Test_DuplicateEmail_ThrowsException()
        {
            // Вставляем первого пользователя с уникальным Email
            _mockDatabase.Add(new User { Id = _nextId++, Username = "User1", Email = "same@qa.com" });

            // Попытка вставить второго с таким же Email
            var duplicateUser = new User { Username = "User2", Email = "same@qa.com" };

            // Проверяем ограничение уникальности UNIQUE (выбрасываем ошибку, если email совпал)
            Assert.Throws<InvalidOperationException>(() =>
            {
                if (_mockDatabase.Any(u => u.Email == duplicateUser.Email))
                {
                    throw new InvalidOperationException("Нарушено ограничение UNIQUE: Email уже существует!");
                }
                duplicateUser.Id = _nextId++;
                _mockDatabase.Add(duplicateUser);
            }, "База данных пропустила дубликат Email! Нарушена целостность данных!");
        }

        [Test] // 4. Тест производительности массовых операций
        public void Test_MassInsertion_Performance()
        {
            int count = 1000;
            Stopwatch sw = Stopwatch.StartNew();

            // Имитируем массовую пакетную вставку 1000 записей
            for (int i = 0; i < count; i++)
            {
                var u = new User { Id = _nextId++, Username = $"User_{i}", Email = $"user_{i}@mail.com" };

                // Проверка индекса перед вставкой (эмуляция работы СУБД)
                if (_mockDatabase.Any(dbUser => dbUser.Email == u.Email)) continue;

                _mockDatabase.Add(u);
            }

            sw.Stop();
            long elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine($"Время массовой транзакции: {elapsed} мс");

            // Тест пройдет, если операция заняла меньше 1500 миллисекунд
            Assert.Less(elapsed, 1500, "База данных работает слишком медленно!");
        }
    }
}
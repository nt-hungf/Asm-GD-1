using Asm_GD_1.Contexts;
using Asm_GD_1.Controllers;
using Asm_GD_1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace FastFoodShop.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private AppDbContext _context;
        private AccountController _controller;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("AccountTestDB")
                .Options;

            _context = new AppDbContext(options);

            _controller = new AccountController(_context);

            // 👇 Gắn Session giả để tránh NullReference
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var hashMethod = _controller.GetType()
                .GetMethod("HashPassword", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var hashedPassword = hashMethod.Invoke(_controller, new object[] { "matkhauhung" }) as string;
            _context.Users.Add(new User
            {
                FullName = "Nguyễn Trọng Hùng",
                Email = "tronghung0708@gmail.com",
                Password = hashedPassword,
                Role = "Admin",
                Address = "tronghung0708@gmail.com",
                Phone = "0768525215"
            });
            // 👇 Thêm user thường (C1, C2)
            var hashedUser = hashMethod.Invoke(_controller, new object[] { "xinchao" }) as string;
            _context.Users.Add(new User
            {
                FullName = "test1",
                Email = "test1@gmail.com",
                Password = hashedUser,
                Role = "User",
                Address = "nhacuatest",
                Phone = "0768525216"
            });

            _context.SaveChanges();
        }


        private string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        // ✅ C1: Đăng nhập hợp lệ (User)
        [Test]
        public void Login_User_Dung()
        {
            var result = _controller.Login("test1@gmail.com", "xinchao") as RedirectToActionResult;

            Assert.That(result, Is.Not.Null, "Không redirect được khi đăng nhập đúng");
            Assert.That(result.ActionName, Is.EqualTo("Index"), "Phải chuyển đến Index");
            Assert.That(result.ControllerName, Is.EqualTo("Home"), "Phải chuyển đến controller Home");
        }

        // ✅ C2: Đăng nhập sai mật khẩu
        [Test]
        public void Login_User_Sai()
        {
            var result = _controller.Login("test1@gmail.com", "xinchao1") as ViewResult;

            Assert.That(result, Is.Not.Null, "Không trả về View khi sai mật khẩu");
            Assert.That(_controller.ViewBag.Error, Is.EqualTo("Sai email hoặc mật khẩu."), "Sai thông báo lỗi");
        }

        [Test]
        public void Login_Admin_Dung()
        {
            var result = _controller.Login("tronghung0708@gmail.com", "matkhauhung") as RedirectToActionResult;

            Assert.That(result, Is.Not.Null, "Không redirect được");
            Assert.That(result.ActionName, Is.EqualTo("Index"), "Sai ActionName");
            Assert.That(result.ControllerName, Is.EqualTo("Home"), "Sai ControllerName");
        }

        [Test]
        public void Login_Admin_Sai()
        {
            var result = _controller.Login("saiemail@gmail.com", "saimk") as ViewResult;

            Assert.That(result, Is.Not.Null, "Không trả về View khi sai tài khoản");
            Assert.That(_controller.ViewBag.Error, Is.EqualTo("Sai email hoặc mật khẩu."), "Sai thông báo lỗi");
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose(); // Dispose the controller to fix NUnit1032
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }

    // ✅ Lớp mô phỏng Session
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new();
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public void Clear() => _sessionStorage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _sessionStorage.Remove(key);
        public void Set(string key, byte[] value) => _sessionStorage[key] = value;
        public bool TryGetValue(string key, out byte[]? value) => _sessionStorage.TryGetValue(key, out value);
    }
}

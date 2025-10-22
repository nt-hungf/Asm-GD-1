using Asm_GD_1.Contexts;
using Asm_GD_1.Controllers;
using Asm_GD_1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asm_GD_1.Tests
{
    [TestFixture]
    public class AdminControllerTests
    {
        private AppDbContext _context;
        private AdminController _controller;

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted(); // 💥 Xóa toàn bộ data sau mỗi test
            _controller?.Dispose();
            _context.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"AdminControllerTests_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);

            _context.Users.AddRange(
                new User { UserId = 1, FullName = "Admin", Role = "Admin", Phone = "0909", Password = "123", Email = "admin@test.com", Address = "HN" },
                new User { UserId = 2, FullName = "User A", Role = "User", Phone = "0908", Password = "456", Email = "user@test.com", Address = "SG" }
            );
            _context.Combos.Add(new Combo { ComboId = 1, Name = "Combo1", Price = 150000, Description = "Combo đặc biệt", ImageUrl = "https://abc.com/combo.jpg" });
            _context.Foods.Add(new Food { FoodId = 1, Name = "Pizza", Price = 100000, Description = "Pizza ngon", ImageUrl = "https://abc.com/pizza.jpg" });
            _context.Orders.Add(new Order { OrderId = 1, UserId = 2, Status = "Chưa giao" });
            _context.SaveChanges();

            _controller = new AdminController(_context);
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new FakeSession();
            httpContext.Session.SetString("UserRole", "Admin");
            httpContext.Session.SetString("UserId", "1");

            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }


        // ========== A3: Cập nhật thông tin admin ==========
        [Test]
        public void EditUser_AdminUpdate_Success()
        {
            var updated = new User { UserId = 1, FullName = "Admin Update", Phone = "0912345678", Role = "Admin", Password = "123" };
            var result = _controller.EditUser(updated) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.That(result.ActionName, Is.EqualTo("Users"));
        }

        // ========== A4: Xem danh sách tài khoản ==========
        [Test]
        public void Users_AdminRole_ShowList()
        {
            var result = _controller.Users() as ViewResult;
            var model = result.Model as List<User>;

            Assert.IsNotNull(model);
            Assert.That(model.Count, Is.EqualTo(2));
        }

        // ========== A5: Xóa tài khoản thường ==========
        [Test]
        public void DeleteUser_NormalUser_DeletedSuccessfully()
        {
            var result = _controller.DeleteConfirmed(2) as RedirectToActionResult;
            Assert.That(result.ActionName, Is.EqualTo("Users"));

            var user = _context.Users.Find(2);
            Assert.IsNull(user);
        }

        // ========== A6: Không cho xóa admin hiện tại ==========
        [Test]
        public void DeleteUser_AdminSelf_Fail()
        {
            var result = _controller.DeleteConfirmed(1) as RedirectToActionResult;

            Assert.That(result.ActionName, Is.EqualTo("Users"));
            Assert.That(_controller.TempData["Error"], Is.EqualTo("Bạn không thể xóa tài khoản đang đăng nhập."));
        }

        // ========== A7: Thêm món ăn ==========
        [Test]
        public void CreateFood_ValidFood_RedirectToMonAn()
        {
            var food = new Food
            {
                Name = "Pizza Gà",
                Price = 100000,
                Description = "Pizza gà phô mai cực ngon",
                ImageUrl = "https://example.com/pizza-ga.jpg"
            };

            var result = _controller.CreateFood(food) as RedirectToActionResult;

            Assert.That(result.ActionName, Is.EqualTo("MonAn"));
        }

        // ========== A8: Sửa combo ==========
        [Test]
        public void EditCombo_UpdatePrice_Success()
        {
            var combo = _context.Combos.FirstOrDefault(c => c.ComboId == 1);
            combo.Price = 120000;
            combo.Description = "Combo đặc biệt";
            combo.ImageUrl = "https://example.com/combo.jpg";

            var result = _controller.EditCombo(combo) as RedirectToActionResult;

            Assert.That(result.ActionName, Is.EqualTo("Combo"));
        }

        // ========== A9: Cập nhật trạng thái đơn hàng ==========
        [Test]
        public void UpdateOrderStatus_ChangeStatus_Success()
        {
            var result = _controller.UpdateOrderStatus(1, "Đã giao") as RedirectToActionResult;

            Assert.That(result.ActionName, Is.EqualTo("Order"));
            var order = _context.Orders.Find(1);
            Assert.That(order.Status, Is.EqualTo("Đã giao"));
        }
    }

    // Giả lập Session
    public class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new();

        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public string Id => "FakeSession";
        public bool IsAvailable => true;

        public void Clear() => _sessionStorage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _sessionStorage.Remove(key);

        public void Set(string key, byte[] value) => _sessionStorage[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
    }
}

using Asm_GD_1.Contexts;
using Asm_GD_1.Controllers;
using Asm_GD_1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // thêm using này
using Moq;

namespace FastFoodShop.Tests
{
    [TestFixture]
    public class HomeControllerTests
    {
        private AppDbContext _context;
        private HomeController _controller;
        private int _foodId;
        private int _userId;

        [SetUp]
        public void Setup()
        {
            // Tạo database ảo (InMemory)
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDB_" + System.Guid.NewGuid())
                .Options;

            _context = new AppDbContext(options);

            // Seed dữ liệu Food
            var food = new Food
            {
                Name = "Pizza",
                Price = 100000,
                Description = "Pizza ngon",
                ImageUrl = "https://abc.com/pizza.jpg"
            };
            _context.Foods.Add(food);

            // Seed dữ liệu User
            var user = new User
            {
                FullName = "test1",
                Email = "test1@gmail.com",
                Password = "xinchao",
                Role = "User",
                Address = "nhacuatest",
                Phone = "0768525216"
            };
            _context.Users.Add(user);

            // Seed Combo
            _context.Combos.Add(new Combo
            {
                Name = "Combo1",
                Price = 150000,
                Description = "Combo đặc biệt",
                ImageUrl = "https://abc.com/combo.jpg"
            });

            _context.SaveChanges();

            // Lấy Id thực tế EF gán
            _foodId = food.FoodId;
            _userId = user.UserId;

            // Tạo HttpContext có Session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new DummySession();

            // Tạo HomeController
            _controller = new HomeController(_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>()) // ✅ Dòng này thêm vào
            };
        }

        // ------------------- C4: Kiểm tra action trả về View -------------------
        [Test]
        public void Index_ReturnsView()
        {
            var result = _controller.Index() as ViewResult;
            Assert.IsNotNull(result);
        }

        [Test]
        public void ChiTietMon_ReturnsCorrectFood()
        {
            var result = _controller.ChiTietMon(_foodId) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Model);
            Assert.IsInstanceOf<Food>(result.Model);

            var food = (Food)result.Model;
            Assert.AreEqual("Pizza", food.Name);
        }

        // ------------------- C5: Giỏ hàng (Session) -------------------
        [Test]
        public void ThemVaoGio_AddsItemToCart()
        {
            _controller.ThemGioHang(_foodId, "food");

            var session = (DummySession)_controller.HttpContext.Session;
            var cart = session.GetObjectFromJson<List<OrderDetail>>("Cart");

            Assert.IsNotNull(cart);
            Assert.AreEqual(1, cart.Count);
            Assert.AreEqual(_foodId, cart[0].FoodId);
        }

        [Test]
        public void XemGioHang_ReturnsCartView()
        {
            _controller.ThemGioHang(_foodId, "food");

            var result = _controller.XemGioHang() as ViewResult;
            Assert.IsNotNull(result);

            var cart = result.Model as List<OrderDetail>;
            Assert.IsNotNull(cart);
            Assert.AreEqual(1, cart.Count);
        }

        // ------------------- C6: Đặt hàng -------------------
        [Test]
        public void DatHang_CreatesOrderAndClearsCart()
        {
            // Giả lập người dùng và giỏ hàng
            _controller.HttpContext.Session.SetInt32("UserId", _userId);
            _controller.ThemGioHang(_foodId, "food");

            var initialCart = ((DummySession)_controller.HttpContext.Session)
                .GetObjectFromJson<List<OrderDetail>>("Cart");
            Assert.IsNotNull(initialCart);
            Assert.IsNotEmpty(initialCart);

            var result = _controller.DatHang("123 Đường ABC", "COD") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Order", result.ActionName);

            // Kiểm tra dữ liệu trong DB
            Assert.AreEqual(1, _context.Orders.Count());
            Assert.AreEqual(1, _context.OrderDetails.Count());

            // Kiểm tra giỏ hàng rỗng
            var session = (DummySession)_controller.HttpContext.Session;
            var clearedCart = session.GetObjectFromJson<List<OrderDetail>>("Cart");
            Assert.IsNull(clearedCart);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose(); // Dispose the HomeController instance
            _context.Dispose();
        }
    }

    // ------------------- Dummy Session -------------------
    public class DummySession : ISession
    {
        private Dictionary<string, byte[]> _sessionStorage = new();

        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public string Id => "dummy";
        public bool IsAvailable => true;

        public void Clear() => _sessionStorage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _sessionStorage.Remove(key);
        public void Set(string key, byte[] value) => _sessionStorage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);

        // String
        public void SetString(string key, string value) =>
            _sessionStorage[key] = System.Text.Encoding.UTF8.GetBytes(value);
        public string GetString(string key) =>
            _sessionStorage.TryGetValue(key, out var data) ? System.Text.Encoding.UTF8.GetString(data) : null;

        // Int32
        public void SetInt32(string key, int value) => SetString(key, value.ToString());
        public int? GetInt32(string key)
        {
            var str = GetString(key);
            if (int.TryParse(str, out int result))
                return result;
            return null;
        }
    }

    // ------------------- Session Extensions -------------------
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            session.SetString(key, json);
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return json == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
    }
}

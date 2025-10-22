    using Asm_GD_1.Contexts;
    using Asm_GD_1.Help;
    using Asm_GD_1.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System.Diagnostics;

    namespace Asm_GD_1.Controllers;

    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var foods = _context.Foods.Where(f => f.IsAvailable).ToList();
            var combos = _context.Combos.ToList();
            ViewBag.Combos = combos;

            return View(foods);
        }

        public IActionResult Cart()
        {
            var cartItems = HttpContext.Session.GetObjectFromJson<List<OrderDetail>>("Cart") ?? new List<OrderDetail>();
            ViewBag.Combos = _context.Combos.ToList(); // Gửi combo ra view
            return View(cartItems);
        }

        private void ClearCart() => HttpContext.Session.Remove("Cart");

        private int GetCurrentUserId()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
                return userId.Value;

            TempData["Error"] = "Bạn cần đăng nhập để thực hiện chức năng này.";
            throw new Exception("Không tìm thấy thông tin UserId trong Claims.");
        }

        [HttpPost]
        public IActionResult DatHang(string ShippingAddress, string PaymentMethod)
        {
        int userId = GetCurrentUserId();
        var cartItems = HttpContext.Session.GetObjectFromJson<List<OrderDetail>>("Cart") ?? new List<OrderDetail>();

        if (cartItems.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng trống!";
            return RedirectToAction("Cart");
        }

        // Tính tổng dựa trên Price đã set khi thêm giỏ hàng
        decimal totalAmount = cartItems.Sum(item => item.Price * item.Quantity);

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.Now,
            PaymentMethod = PaymentMethod == "COD" ? "Tiền mặt" :
                 PaymentMethod == "BankTransfer" ? "Chuyển khoản" :
                 "Thanh toán khác"
,
            ShippingAddress = ShippingAddress,
            TotalAmount = totalAmount,
            Status = "Đang xử lí",
            OrderDetails = cartItems.Select(item => new OrderDetail
            {
                FoodId = item.FoodId > 0 ? item.FoodId : null, // ✅ nếu không có Food thì để null
                ComboId = item.ComboId,                      // Nếu là combo
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList()
        };

        try
        {
            _context.Orders.Add(order);
            _context.SaveChanges();

            TempData["Success"] = "Đặt hàng thành công!";
            ClearCart();
            return RedirectToAction("Order");
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            TempData["Error"] = "Đặt hàng thất bại: " + msg;
            return RedirectToAction("Cart");
        }
    }

        public IActionResult Order()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");

            List<Order> orders;

            if (userRole == "Admin")
            {
                orders = _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Food)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
            else
            {
                orders = _context.Orders
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Food)
                    .Where(o => o.User.Email == userEmail)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }

            return View(orders);
        }

        public IActionResult ThemGioHang(int id, string type = "food")
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<OrderDetail>>("Cart")
                       ?? new List<OrderDetail>();

            if (type == "food")
            {
                var food = _context.Foods.Find(id);
                if (food != null)
                {
                    var item = cart.FirstOrDefault(c => c.FoodId == food.FoodId && c.Price == food.Price);
                    if (item != null)
                        item.Quantity++;
                    else
                        cart.Add(new OrderDetail
                        {
                            FoodId = food.FoodId,
                            Quantity = 1,
                            Price = food.Price,
                            Food = food
                        });
                }
            }
            else if (type == "combo")
            {
                var combo = _context.Combos.Find(id);
                if (combo != null)
                {
                    var item = cart.FirstOrDefault(c => c.FoodId == 0 && c.Price == combo.Price);
                if (item != null)
                    item.Quantity++;
                else
                    cart.Add(new OrderDetail
                    {
                        ComboId = combo.ComboId,
                        Quantity = 1,
                        Price = combo.Price,
                        Combo = combo
                        // Không có Description nên bỏ
                    });
                }
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction("Cart");
        }


        public IActionResult OrderDetail(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Food)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Combo)  // ✅ thêm include Combo
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]                                                                                                                                                          
        public IActionResult XoaGioHang(int? foodId, int? comboId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<OrderDetail>>("Cart")
                       ?? new List<OrderDetail>();

        var item = cart.FirstOrDefault(c =>
        (foodId.HasValue && c.FoodId == foodId) ||
        (comboId.HasValue && c.ComboId == comboId));
        if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
            }

            return RedirectToAction("Cart");
        }

        public IActionResult ChiTietMon(int id)
        {
        var food = _context.Foods.FirstOrDefault(f => f.FoodId == id);
        if (food == null)
            return NotFound();

        return View(food); // ✅ phải có model
    }
    public IActionResult ThemVaoGio(int id, string type = "food")
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<OrderDetail>>("Cart")
                   ?? new List<OrderDetail>();

        if (type == "food")
        {
            var food = _context.Foods.Find(id);
            if (food != null)
            {
                var item = cart.FirstOrDefault(c => c.FoodId == food.FoodId);
                if (item != null)
                    item.Quantity++;
                else
                    cart.Add(new OrderDetail
                    {
                        FoodId = food.FoodId,
                        Quantity = 1,
                        Price = food.Price,
                        Food = food
                    });
            }
        }

        HttpContext.Session.SetObjectAsJson("Cart", cart);
        return RedirectToAction("Cart");
    }
    public IActionResult XemGioHang()
    {
        var cartItems = HttpContext.Session.GetObjectFromJson<List<OrderDetail>>("Cart")
                        ?? new List<OrderDetail>();
        ViewBag.Combos = _context.Combos.ToList();
        return View(cartItems);
    }
    public IActionResult GioiThieu() => View();
        public IActionResult LienHe() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

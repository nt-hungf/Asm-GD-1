using Asm_GD_1.Contexts;
using Asm_GD_1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asm_GD_1.Controllers
{
    public class AdminController : Controller

    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }



        // Kiểm tra quyền truy cập trước mỗi action
        private bool IsAdmin()
        {   
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        public IActionResult Users()
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");
            var users = _context.Users.ToList(); // nếu dùng Entity Framework

            return View(users);
        }




        public async Task<IActionResult> MonAn()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied");
            var foodList = await _context.Foods.Include(f => f.Category).ToListAsync();
            return View(foodList);
        }

        // ================== CREATE ==================
        // GET: Admin/CreateFood
        [HttpGet]
        public IActionResult CreateFood()
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateFood(Food food)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            if (ModelState.IsValid)
            {
                _context.Foods.Add(food);
                _context.SaveChanges();
                return RedirectToAction("MonAn");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(food);
        }

        // ================== EDIT ==================
        [HttpGet]
        public IActionResult EditFood(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            var food = _context.Foods.Find(id);
            if (food == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            return View(food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditFood(Food food)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            if (ModelState.IsValid)
            {
                _context.Foods.Update(food);
                _context.SaveChanges();
                return RedirectToAction("MonAn");
            }
            ViewBag.Categories = _context.Categories.ToList();
            return View(food);
        }


        // ================== DELETE ==================
        [HttpGet]
        public IActionResult DeleteFood(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            var food = _context.Foods.Include(f => f.Category)
                                     .FirstOrDefault(f => f.FoodId == id);
            if (food == null) return NotFound();

            return View(food);
        }

        [HttpPost, ActionName("DeleteFood")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFoodConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");
            var food = _context.Foods.Find(id);
            if (food != null)
            {
                _context.Foods.Remove(food);
                _context.SaveChanges();
            }
            return RedirectToAction("MonAn");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult DonHang()
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            // TODO: Lấy danh sách đơn hàng từ database
            return View();
        }


        // ================== EDITUSER ==================
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
                return NotFound();

            return View(user); // Truyền model vào View
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(User updatedUser)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            // 1) Lấy user hiện có
            var user = _context.Users.FirstOrDefault(u => u.UserId == updatedUser.UserId);
            if (user == null)
                return NotFound();

            // 2) Nếu form không gửi Password -> giữ mật khẩu cũ & bỏ lỗi Required cho Password
            if (string.IsNullOrWhiteSpace(updatedUser.Password))
            {
                updatedUser.Password = user.Password;
                ModelState.Remove(nameof(Asm_GD_1.Models.User.Password)); // rất quan trọng: xóa lỗi trước khi check IsValid
            }

            // 3) Check validate sau khi xử lý Password
            if (!ModelState.IsValid)
            {
                return View(updatedUser);
            }

            // 4) Kiểm tra trùng SĐT với người khác
            var phoneConflict = _context.Users.Any(u => u.Phone == updatedUser.Phone && u.UserId != updatedUser.UserId);
            if (phoneConflict)
            {
                ModelState.AddModelError(nameof(Asm_GD_1.Models.User.Phone), "Số điện thoại đã được sử dụng bởi người khác.");
                return View(updatedUser);
            }

            // 5) Cập nhật
            user.FullName = updatedUser.FullName;
            user.Email = updatedUser.Email;
            user.Phone = updatedUser.Phone;
            user.Address = updatedUser.Address;
            user.Role = updatedUser.Role;
            user.Password = updatedUser.Password; // giữ nguyên nếu không nhập mới

            // EF Core đã tracking 'user', không cần _context.Users.Update(user);
            _context.SaveChanges();

            TempData["Message"] = "Cập nhật người dùng thành công!";
            return RedirectToAction("Users");
        }


        // ================== DELETEUSER ==================
        [HttpGet]
        public IActionResult DeleteUser (int id)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied");

            var user = _context.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            int currentUserId = 0;

            if (!string.IsNullOrWhiteSpace(currentUserIdStr) && int.TryParse(currentUserIdStr, out var parsedId))
            {
                currentUserId = parsedId;
            }

            if (id == currentUserId)
            {
                TempData["Error"] = "Bạn không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction("Users");
            }                                                                                       

            var user = _context.Users.Include(u => u.Orders).FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                TempData["Error"] = "Người dùng không tồn tại.";
                return RedirectToAction("Users");
            }

            // Xóa tất cả orders trước nếu có
            if (user.Orders.Any())
                _context.Orders.RemoveRange(user.Orders);

            _context.Users.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = "Xóa người dùng thành công.";
            return RedirectToAction("Users");
        }




        // ================== COMBO ==================
        public async Task<IActionResult> Combo()
        {
            if (!IsAdmin())return RedirectToAction("AccessDenied");
            // TODO: Lấy danh sách combo từ database
            var comboList = await _context.Combos.ToListAsync();
            return View(comboList);
        }


        // CREATE
        [HttpGet]
        public IActionResult CreateCombo()
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public IActionResult CreateCombo(Combo combo)                                                                               
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied");
            if(ModelState.IsValid)
            {
                _context.Combos.Add(combo);
                _context.SaveChanges();
                return RedirectToAction("Combo");
            }
            return View();
        }


        // EDIT
        public IActionResult EditCombo(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied");

            var combo = _context.Combos.Find(id);
            if (combo == null) return NotFound();
            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCombo(Combo combo)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied");

            if (ModelState.IsValid)
            {
                _context.Combos.Update(combo);
                _context.SaveChanges();
                return RedirectToAction("Combo");
            }
            return View(combo);
        }


        // DELETE
        public IActionResult DeleteCombo (int id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied");
            var combo = _context.Combos.Find(id);
            if (combo == null) return NotFound();
            return View(combo);
        }
        [HttpPost, ActionName("DeleteCombo")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComboConfirmed (int id)
        {
            if (!IsAdmin()) return RedirectToAction("AccessDenied");
            var combo = _context.Combos.Find(id);
            if (combo != null)
            {
                _context.Combos.Remove(combo);
                _context.SaveChanges();
            }
            return RedirectToAction("Combo");
        }


        // ================== ORDERS ========================
        public async Task<IActionResult> Order ()
        {
            var orders = await _context.Orders.Include(o => o.User).Include(o => o.OrderDetails).ThenInclude(od => od.Food).ToListAsync();
            return View(orders);
        }
        // ================== ORDERSDETAIL ==================
        public IActionResult OrderDetails (int id)
        {
            var order = _context.Orders.Include(o => o.User).Include(o => o.OrderDetails).ThenInclude(od => od.Food).Include(o => o.OrderDetails).ThenInclude(od => od.Combo).FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();
            return View(order);
        }
        // ================== UPDATEORDERS ==================
        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;  
            _context.SaveChanges();

            TempData["Message"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("Order", new { id });
        }
    }
}

using Asm_GD_1.Contexts;
using Asm_GD_1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Asm_GD_1.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // Hiển thị form đăng ký
        [HttpGet]
        public IActionResult Register() => View();

        // Xử lý đăng ký
        [HttpPost]
        public IActionResult Register(User user, string confirmPassword)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            if (user.Password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View(user);
            }

            var existingEmail = _context.Users.FirstOrDefault(u => u.Email == user.Email);
            if (existingEmail != null)
            {
                ViewBag.Error = "Email đã tồn tại.";
                return View(user);
            }

            var existingPhone = _context.Users.FirstOrDefault(u => u.Phone == user.Phone);
            if (existingPhone != null)
            {
                ViewBag.Error = "Số điện thoại đã được sử dụng.";
                return View(user);
            }

            user.Password = HashPassword(user.Password); // Mã hóa mật khẩu
            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // Hiển thị form đăng nhập
        [HttpGet]
        public IActionResult Login() => View();

        // Xử lý đăng nhập
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            string hashedPassword = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == hashedPassword);

            if (user == null)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null) return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null) return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return RedirectToAction("Login");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(int id, User updatedUser)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (currentUser == null || currentUser.UserId != id)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View(updatedUser);
            }

            // Kiểm tra trùng số điện thoại với người khác
            var phoneConflict = await _context.Users
                .AnyAsync(u => u.Phone == updatedUser.Phone && u.UserId != currentUser.UserId);

            if (phoneConflict)
            {
                ViewBag.Error = "Số điện thoại đã được sử dụng bởi người khác.";
                return View(updatedUser);
            }

            currentUser.FullName = updatedUser.FullName;
            currentUser.Email = updatedUser.Email;
            currentUser.Phone = updatedUser.Phone;
            currentUser.Address = updatedUser.Address;
            currentUser.Role = updatedUser.Role;

            _context.Users.Update(currentUser);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserEmail", currentUser.Email);
            HttpContext.Session.SetString("FullName", currentUser.FullName);
            HttpContext.Session.SetString("UserRole", currentUser.Role);

            return RedirectToAction("Profile");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        // Mã hóa mật khẩu bằng SHA256
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
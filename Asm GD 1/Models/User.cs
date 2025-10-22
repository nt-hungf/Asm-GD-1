using System.ComponentModel.DataAnnotations;

namespace Asm_GD_1.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vai trò không được để trống")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Tên không được để trống")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 kí tự")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 kí tự trở lên")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Địa chỉ trên 6 kí tự")]
        public string Address { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải 10 chữ số")]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get;set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

namespace Asm_GD_1.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }

        // nullable hoặc khởi tạo mặc định
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "COD";

        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;

        // Foreign Key
        public int UserId { get; set; }
        public User User { get; set; } = null!; // EF hiểu là bắt buộc

        // Quan hệ
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}

namespace Asm_GD_1.Models
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int Quantity { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int? FoodId { get; set; }
        public Food? Food { get; set; } = null!;

        public int? ComboId {  get; set; }
        public Combo? Combo { get; set; }

        public decimal Price { get; set; }
    }
}
